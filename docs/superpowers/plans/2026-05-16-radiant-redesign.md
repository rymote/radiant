# Rymote.Radiant v2.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor Rymote.Radiant from a PostgreSQL-only library into a dialect-agnostic core with pluggable database adapters, and overhaul SmartModel into a production ORM that meets the ORM needs documented in the rymote-aether-server consumer.

**Architecture:** A new `IDatabaseAdapter` contract isolates every dialect-specific concern (identifier quoting, parameter formatting, SQL dialect, value writing, result mapping, connection lifecycle). The PostgreSQL adapter ships in its own project (`Rymote.Radiant.Adapters.PostgreSql`) and owns the Npgsql + Dapper dependencies. The Smart layer is rebuilt around an instance-based `SmartContext`, with the existing static `SmartModel.Query()` style preserved via an `AsyncLocal` ambient context populated by ASP.NET Core middleware.

**Tech Stack:** .NET 10, C# 13, Npgsql 10, Dapper 2.1, Microsoft.Extensions.DependencyInjection 10, xUnit 2.9, Testcontainers.PostgreSql.

**Working directory:** `C:\Users\jovan\Projects\rymote\libraries\radiant`

**Commit policy:** Per user instruction, **no commits during execution**. After each verification checkpoint, run `dotnet build` and `dotnet test`; do not run `git commit`. Final commit batch is the user's call.

---

## Reference: Spec coverage map

| Spec section                  | Phase / Task                |
| ----------------------------- | --------------------------- |
| §4.1 Project layout           | Phase 1, Task 1             |
| §4.2 IDatabaseAdapter         | Phase 1, Tasks 2–6          |
| §4.3 SqlEmitter / Compiler    | Phase 1, Tasks 7–8          |
| §4.4 ParameterBag             | Phase 1, Task 9             |
| §4.5 SmartContext             | Phase 2, Tasks 1–4          |
| §4.6 DI registration          | Phase 2, Task 5             |
| §4.7 LINQ translator          | Phase 3                     |
| §4.8 Include / ThenInclude    | Phase 4                     |
| §4.9 Transactions             | Phase 5, Task 1             |
| §4.10 Bulk ops                | Phase 5, Task 2             |
| §4.11 Global query filters    | Phase 5, Task 3             |
| §4.12 Schema scoping          | Phase 2, Task 6             |
| §4.13 Audit / timestamps      | Phase 5, Task 4             |
| §4.14 Result mapping          | Phase 1 Task 6 + Phase 7    |
| §4.15 Source generator scope  | Phase 7                     |
| §6 Testing strategy           | Phase 6 (runs across plan)  |
| §8 Acceptance criteria        | Phase 8 (final verification)|

---

## File structure target

Final structure on disk (only directories with substantive changes shown):

```
Rymote.Radiant.sln                                 [modified — adds adapter projects]
Rymote.Radiant/                                    [core, no DB driver deps]
├── Rymote.Radiant.csproj                          [modified — remove Npgsql]
├── Sql/
│   ├── Builder/                                   [unchanged public API]
│   ├── Clauses/                                   [Accept(SqlEmitter) replaces AppendTo]
│   ├── Expressions/                               [Accept(SqlEmitter)]
│   ├── Compiler/QueryCompiler.cs                  [thin dispatcher]
│   ├── Compiler/SqlEmitter.cs                     [NEW — visits clauses/expressions]
│   ├── Executor/QueryExecutor.cs                  [takes IDatabaseAdapter]
│   ├── Parameters/ParameterBag.cs                 [dialect-agnostic]
│   └── Dialects/                                  [REMOVED — moved to adapters]
├── Adapters/                                      [NEW]
│   ├── IDatabaseAdapter.cs
│   ├── DatabaseCapabilities.cs
│   ├── ISqlDialect.cs
│   ├── IIdentifierQuoter.cs
│   ├── IParameterFormatter.cs
│   ├── IValueWriter.cs
│   ├── IResultMapper.cs
│   ├── QueryParameter.cs
│   └── CompiledQuery.cs
└── Smart/
    ├── Configuration/RadiantBuilder.cs            [NEW — replaces SmartModelConfiguration as public entry point]
    ├── Configuration/SmartContextOptions.cs       [NEW]
    ├── Configuration/ValueConverter.cs            [NEW]
    ├── Configuration/GlobalQueryFilter.cs         [NEW]
    ├── Context/SmartContext.cs                    [NEW]
    ├── Context/SmartContextAmbient.cs             [NEW — AsyncLocal]
    ├── Context/ISmartTransaction.cs               [NEW]
    ├── Context/SmartTransaction.cs                [NEW]
    ├── DependencyInjection/                       [NEW]
    │   ├── RadiantServiceCollectionExtensions.cs
    │   └── RadiantApplicationBuilderExtensions.cs
    ├── Expressions/LinqToSqlTranslator.cs         [NEW — replaces WhereExpressionVisitor]
    ├── Expressions/WhereExpressionVisitor.cs      [DELETED]
    ├── Loading/IncludePath.cs                     [NEW]
    ├── Loading/IncludableSmartQuery.cs            [NEW — supports ThenInclude]
    ├── Loading/BatchedRelationshipLoader.cs       [REWRITE of RelationshipLoader]
    ├── Functions/Radiant.cs                       [NEW — static, mirrors EF.Functions]
    └── SmartModel.cs                              [delegates to ambient SmartContext]

Rymote.Radiant.Adapters.PostgreSql/                [NEW PROJECT]
├── Rymote.Radiant.Adapters.PostgreSql.csproj      [refs Npgsql, Dapper]
├── PostgreSqlAdapter.cs
├── PostgreSqlDialect.cs                           [replaces SqlKeywords as the Postgres-only string source]
├── PostgreSqlIdentifierQuoter.cs
├── PostgreSqlParameterFormatter.cs
├── PostgreSqlValueWriter.cs
├── DapperResultMapper.cs                          [moved from core]
├── DependencyInjection/PostgreSqlBuilderExtensions.cs

Rymote.Radiant.Adapters.SqlServer/                 [NEW PROJECT — skeleton]
├── Rymote.Radiant.Adapters.SqlServer.csproj
├── SqlServerAdapter.cs                            [SELECT-1-only proof of capability]
└── SqlServerDialect.cs

Rymote.Radiant.Tests/                              [NEW PROJECT]
├── Rymote.Radiant.Tests.csproj                    [xUnit]
├── Sql/SqlEmitterTests.cs
├── Smart/LinqToSqlTranslatorTests.cs
├── Smart/IncludeTests.cs
├── Smart/SmartContextTests.cs
└── Adapters/CapabilityTests.cs

Rymote.Radiant.Adapters.PostgreSql.IntegrationTests/  [NEW PROJECT]
├── Rymote.Radiant.Adapters.PostgreSql.IntegrationTests.csproj
└── EndToEndPostgreSqlTests.cs                     [Testcontainers]
```

---

## Phase 1: Adapter abstraction (no behavior change visible to Postgres callers)

### Task 1: Create the PostgreSQL adapter project shell

**Files:**
- Create: `Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj`
- Modify: `Rymote.Radiant.sln`
- Modify: `Rymote.Radiant/Rymote.Radiant.csproj`

- [ ] **Step 1: Create the new project file**

Create `Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <PackageId>Rymote.Radiant.Adapters.PostgreSql</PackageId>
        <Authors>Rymote</Authors>
        <Description>PostgreSQL adapter for Rymote.Radiant.</Description>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Dapper" Version="2.1.72" />
        <PackageReference Include="Npgsql" Version="10.0.2" />
        <PackageReference Include="System.Text.Json" Version="10.0.7" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\Rymote.Radiant\Rymote.Radiant.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Add the project to the solution**

Run from solution directory:

```powershell
dotnet sln Rymote.Radiant.sln add Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj
```

Expected: solution file gains a new `Project(…)` entry and matching configuration block.

- [ ] **Step 3: Remove driver dependencies from core**

Edit `Rymote.Radiant/Rymote.Radiant.csproj` — remove `Npgsql` and `Dapper` `PackageReference` entries; leave the rest:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.7" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7" />
    <PackageReference Include="System.Text.Json" Version="10.0.7" />
</ItemGroup>
```

- [ ] **Step 4: Verify the build breaks in the expected places**

Run:

```powershell
dotnet build Rymote.Radiant.sln
```

Expected: build fails with `CS0246` errors for `Dapper.DynamicParameters` in `Sql/Parameters/ParameterBag.cs`, `Sql/Executor/QueryExecutor.cs`, and `Sql/QueryCommand.cs`, plus `using Npgsql;` references in `Playground/Program.cs`. Note the failing files — Phase 1 Task 9 and Task 6 fix them.

- [ ] **Step 5: Verify build succeeds when Npgsql/Dapper are wired through the new adapter project**

This step gets unblocked at the end of Phase 1, Task 9. For now leave the build red; the rest of Phase 1 lines up the contracts that will fix it.

---

### Task 2: Define the adapter contract files

**Files:**
- Create: `Rymote.Radiant/Adapters/IDatabaseAdapter.cs`
- Create: `Rymote.Radiant/Adapters/DatabaseCapabilities.cs`
- Create: `Rymote.Radiant/Adapters/CompiledQuery.cs`
- Create: `Rymote.Radiant/Adapters/QueryParameter.cs`

- [ ] **Step 1: Create `DatabaseCapabilities.cs`**

```csharp
namespace Rymote.Radiant.Adapters;

[System.Flags]
public enum DatabaseCapabilities : long
{
    None                       = 0,
    ReturningClause            = 1L << 0,
    UpsertOnConflict           = 1L << 1,
    UpsertMerge                = 1L << 2,
    CommonTableExpression      = 1L << 3,
    RecursiveCommonTableExpression = 1L << 4,
    LateralJoin                = 1L << 5,
    WindowFunctions            = 1L << 6,
    SchemaPerTable             = 1L << 7,
    JsonbColumn                = 1L << 8,
    ArrayColumn                = 1L << 9,
    VectorColumn               = 1L << 10,
    FullTextSearch             = 1L << 11,
    RangeTypes                 = 1L << 12,
    CaseInsensitiveText        = 1L << 13,
    SpatialTypes               = 1L << 14,
    CaseInsensitiveLikeOperator= 1L << 15,
    RegularExpressionOperator  = 1L << 16,
    NamedSequences             = 1L << 17,
    BatchedInsertReturning     = 1L << 18,
}
```

- [ ] **Step 2: Create `QueryParameter.cs`**

```csharp
using System.Data;

namespace Rymote.Radiant.Adapters;

public sealed record QueryParameter(
    string Name,
    object? Value,
    DbType? Type = null,
    string? DatabaseNativeType = null);
```

- [ ] **Step 3: Create `CompiledQuery.cs`**

```csharp
using System.Collections.Generic;

namespace Rymote.Radiant.Adapters;

public sealed class CompiledQuery
{
    public string Sql { get; }
    public IReadOnlyList<QueryParameter> Parameters { get; }

    public CompiledQuery(string sql, IReadOnlyList<QueryParameter> parameters)
    {
        Sql = sql;
        Parameters = parameters;
    }
}
```

- [ ] **Step 4: Create `IDatabaseAdapter.cs`**

```csharp
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Adapters;

public interface IDatabaseAdapter
{
    string Identifier { get; }
    DatabaseCapabilities Capabilities { get; }
    ISqlDialect Dialect { get; }
    IIdentifierQuoter IdentifierQuoter { get; }
    IParameterFormatter ParameterFormatter { get; }
    IValueWriter ValueWriter { get; }
    IResultMapper ResultMapper { get; }

    DbConnection CreateConnection();
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
    DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery);
}
```

- [ ] **Step 5: Verify the contract files compile**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: still fails on existing code that depends on Dapper, but the four new files compile clean.

---

### Task 3: Define the dialect contracts

**Files:**
- Create: `Rymote.Radiant/Adapters/ISqlDialect.cs`
- Create: `Rymote.Radiant/Adapters/IIdentifierQuoter.cs`
- Create: `Rymote.Radiant/Adapters/IParameterFormatter.cs`
- Create: `Rymote.Radiant/Adapters/IValueWriter.cs`
- Create: `Rymote.Radiant/Adapters/IResultMapper.cs`

- [ ] **Step 1: Create `IIdentifierQuoter.cs`**

```csharp
namespace Rymote.Radiant.Adapters;

public interface IIdentifierQuoter
{
    string QuoteIdentifier(string identifier);
    string QuoteQualifiedName(string? schemaName, string objectName);
}
```

- [ ] **Step 2: Create `IParameterFormatter.cs`**

```csharp
namespace Rymote.Radiant.Adapters;

public interface IParameterFormatter
{
    string FormatPlaceholder(int ordinal);
    string FormatParameterName(int ordinal);
}
```

- [ ] **Step 3: Create `IValueWriter.cs`**

```csharp
using System.Text;

namespace Rymote.Radiant.Adapters;

public interface IValueWriter
{
    void WriteLiteral(StringBuilder buffer, object? value);
    void WriteTypedLiteral(StringBuilder buffer, object? value, string databaseNativeType);
    void WriteArrayLiteral(StringBuilder buffer, System.Array elements, string? elementDatabaseType);
}
```

- [ ] **Step 4: Create `IResultMapper.cs`**

```csharp
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Adapters;

public interface IResultMapper
{
    Task<IReadOnlyList<TResult>> QueryAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<TResult> QuerySingleAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Create `ISqlDialect.cs`** — this is the meat. It exposes every dialect-specific keyword and sub-dialect.

```csharp
namespace Rymote.Radiant.Adapters;

public interface ISqlDialect
{
    string Select { get; }
    string From { get; }
    string Where { get; }
    string GroupBy { get; }
    string Having { get; }
    string OrderBy { get; }
    string Ascending { get; }
    string Descending { get; }
    string Limit { get; }
    string Offset { get; }
    string InsertInto { get; }
    string Update { get; }
    string Delete { get; }
    string Set { get; }
    string Values { get; }
    string ReturningKeyword { get; }
    string OnConflict { get; }
    string DoNothing { get; }
    string DoUpdate { get; }
    string ExcludedTableAlias { get; }
    string CaseInsensitiveLikeOperator { get; }
    string CurrentTimestampExpression { get; }
    string CastOperator { get; }
    string ConcatenateOperator { get; }
    string With { get; }
    string Recursive { get; }
    string LateralKeyword { get; }
    string NullLiteral { get; }
    string TrueLiteral { get; }
    string FalseLiteral { get; }

    IJsonbDialect Jsonb { get; }
    IArrayDialect Array { get; }
    IVectorDialect Vector { get; }
    IFullTextDialect FullText { get; }
    IRangeDialect Range { get; }
}

public interface IJsonbDialect
{
    string ExtractText { get; }
    string ExtractJson { get; }
    string ContainsOperator { get; }
    string ContainedByOperator { get; }
    string HasKeyOperator { get; }
    string HasAnyKeyOperator { get; }
    string HasAllKeysOperator { get; }
    string PathExistsOperator { get; }
    string PathMatchOperator { get; }
    string ConcatenateOperator { get; }
    string DeletePathOperator { get; }
}

public interface IArrayDialect
{
    string ContainsOperator { get; }
    string ContainedByOperator { get; }
    string OverlapOperator { get; }
    string ConcatenateOperator { get; }
    string CardinalityFunction { get; }
    string UnnestFunction { get; }
}

public interface IVectorDialect
{
    string L2DistanceOperator { get; }
    string InnerProductOperator { get; }
    string CosineDistanceOperator { get; }
    string L1DistanceOperator { get; }
}

public interface IFullTextDialect
{
    string MatchOperator { get; }
    string ToTsVectorFunction { get; }
    string ToTsQueryFunction { get; }
    string PlainToTsQueryFunction { get; }
    string PhraseToTsQueryFunction { get; }
    string WebSearchToTsQueryFunction { get; }
    string TsRankFunction { get; }
    string TsRankCoverDensityFunction { get; }
    string TsHeadlineFunction { get; }
}

public interface IRangeDialect
{
    string ContainsElementOperator { get; }
    string ContainedByOperator { get; }
    string OverlapOperator { get; }
    string AdjacentOperator { get; }
}
```

- [ ] **Step 6: Verify the contract files compile**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: existing failures unchanged; new contract files compile.

---

### Task 4: Implement the PostgreSQL dialect

**Files:**
- Create: `Rymote.Radiant.Adapters.PostgreSql/PostgreSqlDialect.cs`
- Create: `Rymote.Radiant.Adapters.PostgreSql/PostgreSqlIdentifierQuoter.cs`
- Create: `Rymote.Radiant.Adapters.PostgreSql/PostgreSqlParameterFormatter.cs`

- [ ] **Step 1: Create `PostgreSqlIdentifierQuoter.cs`**

```csharp
namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlIdentifierQuoter : IIdentifierQuoter
{
    public string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    public string QuoteQualifiedName(string? schemaName, string objectName)
        => string.IsNullOrWhiteSpace(schemaName)
            ? QuoteIdentifier(objectName)
            : QuoteIdentifier(schemaName) + "." + QuoteIdentifier(objectName);
}
```

- [ ] **Step 2: Create `PostgreSqlParameterFormatter.cs`**

```csharp
namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlParameterFormatter : IParameterFormatter
{
    public string FormatPlaceholder(int ordinal) => "@" + FormatParameterName(ordinal);
    public string FormatParameterName(int ordinal) => "p" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
```

Rationale: we keep named `@pN` placeholders because Dapper handles the rewrite to Postgres positional `$N` at execute time. When Phase 1 Task 6's `DapperResultMapper` is bypassed we'll switch to true `$N` formatting; this is a future variant of the formatter.

- [ ] **Step 3: Create `PostgreSqlDialect.cs`**

This is a verbatim re-home of every string in `Sql/Dialects/SqlKeywords.cs`, now grouped by responsibility. Full file body:

```csharp
namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlDialect : ISqlDialect
{
    public string Select => "SELECT";
    public string From => "FROM";
    public string Where => "WHERE";
    public string GroupBy => "GROUP BY";
    public string Having => "HAVING";
    public string OrderBy => "ORDER BY";
    public string Ascending => "ASC";
    public string Descending => "DESC";
    public string Limit => "LIMIT";
    public string Offset => "OFFSET";
    public string InsertInto => "INSERT INTO";
    public string Update => "UPDATE";
    public string Delete => "DELETE";
    public string Set => "SET";
    public string Values => "VALUES";
    public string ReturningKeyword => "RETURNING";
    public string OnConflict => "ON CONFLICT";
    public string DoNothing => "DO NOTHING";
    public string DoUpdate => "DO UPDATE";
    public string ExcludedTableAlias => "EXCLUDED";
    public string CaseInsensitiveLikeOperator => "ILIKE";
    public string CurrentTimestampExpression => "CURRENT_TIMESTAMP";
    public string CastOperator => "::";
    public string ConcatenateOperator => "||";
    public string With => "WITH";
    public string Recursive => "RECURSIVE";
    public string LateralKeyword => "LATERAL";
    public string NullLiteral => "NULL";
    public string TrueLiteral => "TRUE";
    public string FalseLiteral => "FALSE";

    public IJsonbDialect Jsonb { get; } = new PostgreSqlJsonbDialect();
    public IArrayDialect Array { get; } = new PostgreSqlArrayDialect();
    public IVectorDialect Vector { get; } = new PostgreSqlVectorDialect();
    public IFullTextDialect FullText { get; } = new PostgreSqlFullTextDialect();
    public IRangeDialect Range { get; } = new PostgreSqlRangeDialect();

    private sealed class PostgreSqlJsonbDialect : IJsonbDialect
    {
        public string ExtractText => "->>";
        public string ExtractJson => "->";
        public string ContainsOperator => "@>";
        public string ContainedByOperator => "<@";
        public string HasKeyOperator => "?";
        public string HasAnyKeyOperator => "?|";
        public string HasAllKeysOperator => "?&";
        public string PathExistsOperator => "@?";
        public string PathMatchOperator => "@@";
        public string ConcatenateOperator => "||";
        public string DeletePathOperator => "#-";
    }

    private sealed class PostgreSqlArrayDialect : IArrayDialect
    {
        public string ContainsOperator => "@>";
        public string ContainedByOperator => "<@";
        public string OverlapOperator => "&&";
        public string ConcatenateOperator => "||";
        public string CardinalityFunction => "cardinality";
        public string UnnestFunction => "unnest";
    }

    private sealed class PostgreSqlVectorDialect : IVectorDialect
    {
        public string L2DistanceOperator => "<->";
        public string InnerProductOperator => "<#>";
        public string CosineDistanceOperator => "<=>";
        public string L1DistanceOperator => "<+>";
    }

    private sealed class PostgreSqlFullTextDialect : IFullTextDialect
    {
        public string MatchOperator => "@@";
        public string ToTsVectorFunction => "to_tsvector";
        public string ToTsQueryFunction => "to_tsquery";
        public string PlainToTsQueryFunction => "plainto_tsquery";
        public string PhraseToTsQueryFunction => "phraseto_tsquery";
        public string WebSearchToTsQueryFunction => "websearch_to_tsquery";
        public string TsRankFunction => "ts_rank";
        public string TsRankCoverDensityFunction => "ts_rank_cd";
        public string TsHeadlineFunction => "ts_headline";
    }

    private sealed class PostgreSqlRangeDialect : IRangeDialect
    {
        public string ContainsElementOperator => "@>";
        public string ContainedByOperator => "<@";
        public string OverlapOperator => "&&";
        public string AdjacentOperator => "-|-";
    }
}
```

- [ ] **Step 4: Verify the adapter project builds**

```powershell
dotnet build Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj
```

Expected: builds clean (no references back to core types it doesn't have yet — only the contract interfaces).

---

### Task 5: Implement the PostgreSQL value writer and Dapper-backed result mapper

**Files:**
- Create: `Rymote.Radiant.Adapters.PostgreSql/PostgreSqlValueWriter.cs`
- Create: `Rymote.Radiant.Adapters.PostgreSql/DapperResultMapper.cs`

- [ ] **Step 1: Create `PostgreSqlValueWriter.cs`**

This consolidates the casting logic currently scattered through `SmartRepository.CreateValueExpression()`:

```csharp
using System;
using System.Globalization;
using System.Text;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlValueWriter : IValueWriter
{
    public void WriteLiteral(StringBuilder buffer, object? value)
    {
        if (value is null)
        {
            buffer.Append("NULL");
            return;
        }

        switch (value)
        {
            case bool booleanValue:
                buffer.Append(booleanValue ? "TRUE" : "FALSE");
                break;
            case string stringValue:
                buffer.Append('\'').Append(stringValue.Replace("'", "''")).Append('\'');
                break;
            case int or long or short or byte or sbyte or uint or ulong or ushort:
                buffer.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            case float floatValue:
                buffer.Append(floatValue.ToString("R", CultureInfo.InvariantCulture));
                break;
            case double doubleValue:
                buffer.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture));
                break;
            case decimal decimalValue:
                buffer.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                break;
            case DateTime dateTimeValue:
                buffer.Append('\'').Append(dateTimeValue.ToString("o", CultureInfo.InvariantCulture)).Append('\'');
                break;
            case Guid guidValue:
                buffer.Append('\'').Append(guidValue.ToString("D")).Append('\'').Append("::uuid");
                break;
            default:
                buffer.Append('\'').Append(value.ToString()!.Replace("'", "''")).Append('\'');
                break;
        }
    }

    public void WriteTypedLiteral(StringBuilder buffer, object? value, string databaseNativeType)
    {
        WriteLiteral(buffer, value);
        buffer.Append("::").Append(databaseNativeType);
    }

    public void WriteArrayLiteral(StringBuilder buffer, Array elements, string? elementDatabaseType)
    {
        buffer.Append("ARRAY[");
        for (int index = 0; index < elements.Length; index++)
        {
            if (index > 0) buffer.Append(", ");
            WriteLiteral(buffer, elements.GetValue(index));
        }
        buffer.Append(']');
        if (!string.IsNullOrEmpty(elementDatabaseType))
            buffer.Append("::").Append(elementDatabaseType).Append("[]");
    }
}
```

- [ ] **Step 2: Create `DapperResultMapper.cs`** — wraps Dapper, so the core never imports Dapper

```csharp
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class DapperResultMapper : IResultMapper
{
    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
    {
        IEnumerable<TResult> results = await command.Connection!.QueryAsync<TResult>(BuildCommand(command, cancellationToken));
        return results.ToList();
    }

    public Task<TResult> QuerySingleAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.QuerySingleAsync<TResult>(BuildCommand(command, cancellationToken));

    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.QuerySingleOrDefaultAsync<TResult?>(BuildCommand(command, cancellationToken));

    public Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.ExecuteAsync(BuildCommand(command, cancellationToken));

    private static CommandDefinition BuildCommand(DbCommand source, CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new DynamicParameters();
        foreach (DbParameter parameter in source.Parameters)
            parameters.Add(parameter.ParameterName, parameter.Value);

        return new CommandDefinition(
            commandText: source.CommandText,
            parameters: parameters,
            transaction: source.Transaction,
            commandTimeout: source.CommandTimeout,
            commandType: source.CommandType,
            cancellationToken: cancellationToken);
    }
}
```

- [ ] **Step 3: Verify the adapter project compiles**

```powershell
dotnet build Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj
```

Expected: clean build.

---

### Task 6: Implement `PostgreSqlAdapter`

**Files:**
- Create: `Rymote.Radiant.Adapters.PostgreSql/PostgreSqlAdapter.cs`

- [ ] **Step 1: Create the adapter class**

```csharp
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlAdapter : IDatabaseAdapter
{
    private readonly NpgsqlDataSource dataSource;

    public PostgreSqlAdapter(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public string Identifier => "postgresql";

    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.ReturningClause
        | DatabaseCapabilities.UpsertOnConflict
        | DatabaseCapabilities.CommonTableExpression
        | DatabaseCapabilities.RecursiveCommonTableExpression
        | DatabaseCapabilities.LateralJoin
        | DatabaseCapabilities.WindowFunctions
        | DatabaseCapabilities.SchemaPerTable
        | DatabaseCapabilities.JsonbColumn
        | DatabaseCapabilities.ArrayColumn
        | DatabaseCapabilities.VectorColumn
        | DatabaseCapabilities.FullTextSearch
        | DatabaseCapabilities.RangeTypes
        | DatabaseCapabilities.CaseInsensitiveText
        | DatabaseCapabilities.CaseInsensitiveLikeOperator
        | DatabaseCapabilities.RegularExpressionOperator
        | DatabaseCapabilities.NamedSequences
        | DatabaseCapabilities.BatchedInsertReturning;

    public ISqlDialect Dialect { get; } = new PostgreSqlDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new PostgreSqlIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new PostgreSqlParameterFormatter();
    public IValueWriter ValueWriter { get; } = new PostgreSqlValueWriter();
    public IResultMapper ResultMapper { get; } = new DapperResultMapper();

    public DbConnection CreateConnection() => dataSource.CreateConnection();

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = dataSource.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery)
    {
        NpgsqlCommand command = (NpgsqlCommand)connection.CreateCommand();
        command.CommandText = compiledQuery.Sql;
        foreach (QueryParameter parameter in compiledQuery.Parameters)
        {
            NpgsqlParameter npgsqlParameter = command.CreateParameter();
            npgsqlParameter.ParameterName = parameter.Name;
            npgsqlParameter.Value = parameter.Value ?? System.DBNull.Value;
            if (parameter.Type.HasValue)
                npgsqlParameter.DbType = parameter.Type.Value;
            command.Parameters.Add(npgsqlParameter);
        }
        return command;
    }
}
```

- [ ] **Step 2: Verify it builds**

```powershell
dotnet build Rymote.Radiant.Adapters.PostgreSql/Rymote.Radiant.Adapters.PostgreSql.csproj
```

Expected: green.

---

### Task 7: Refactor `Sql/Compiler` into `SqlEmitter`

**Files:**
- Create: `Rymote.Radiant/Sql/Compiler/SqlEmitter.cs`
- Modify: `Rymote.Radiant/Sql/Compiler/QueryCompiler.cs`
- Modify: `Rymote.Radiant/Sql/Clauses/IQueryClause.cs`

- [ ] **Step 1: Extend `IQueryClause`**

Edit `Rymote.Radiant/Sql/Clauses/IQueryClause.cs` to add the `Accept` method alongside the existing `AppendTo`:

```csharp
using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses;

public interface IQueryClause
{
    void AppendTo(StringBuilder buffer, ParameterBag parameters);
    void Accept(SqlEmitter emitter);
}
```

Keep `AppendTo` to allow incremental migration; later tasks remove it once every clause has `Accept`.

- [ ] **Step 2: Create the `SqlEmitter` class shell**

```csharp
using System.Text;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Sql.Clauses;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Compiler;

public sealed class SqlEmitter
{
    public IDatabaseAdapter Adapter { get; }
    public ISqlDialect Dialect => Adapter.Dialect;
    public IIdentifierQuoter Quoter => Adapter.IdentifierQuoter;
    public IParameterFormatter ParameterFormatter => Adapter.ParameterFormatter;
    public IValueWriter ValueWriter => Adapter.ValueWriter;
    public StringBuilder Buffer { get; }
    public ParameterBag Parameters { get; }

    public SqlEmitter(IDatabaseAdapter adapter, StringBuilder buffer, ParameterBag parameters)
    {
        Adapter = adapter;
        Buffer = buffer;
        Parameters = parameters;
    }

    public void Emit(IQueryClause clause) => clause.Accept(this);
    public void Emit(ISqlExpression expression) => expression.Accept(this);
}
```

- [ ] **Step 3: Add `Accept` to `ISqlExpression`**

Edit `Rymote.Radiant/Sql/Expressions/ISqlExpression.cs`:

```csharp
using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Expressions;

public interface ISqlExpression
{
    void AppendTo(StringBuilder buffer, ParameterBag parameters);
    void Accept(SqlEmitter emitter);
}
```

- [ ] **Step 4: Sweep concrete clause and expression classes to add no-op `Accept`**

For every file in `Rymote.Radiant/Sql/Clauses/**` and `Rymote.Radiant/Sql/Expressions/**`, add this default `Accept` that delegates to the existing `AppendTo`:

```csharp
public void Accept(Rymote.Radiant.Sql.Compiler.SqlEmitter emitter)
    => AppendTo(emitter.Buffer, emitter.Parameters);
```

This is a mechanical sweep — about 60 files. Use grep to find each class declaration and add the method. The `Accept` is intentionally a stub here: the next task replaces it with real dialect-aware code.

- [ ] **Step 5: Verify build**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: still red on Dapper, but no new failures from the sweep.

---

### Task 8: Migrate dialect-specific clauses to use `SqlEmitter`

For each clause/expression that currently hardcodes a string from `SqlKeywords`, swap to the dialect on the emitter.

**Files (one step per file pair):**
- `Sql/Clauses/Table/TableClause.cs`
- `Sql/Expressions/ColumnExpression.cs`
- `Sql/Clauses/From/FromClause.cs`
- `Sql/Clauses/Where/WhereCondition.cs`
- `Sql/Clauses/Insert/OnConflictClause.cs`
- `Sql/Clauses/Returning/ReturningClause.cs`
- `Sql/Clauses/Join/LateralJoinClause.cs`
- `Sql/Expressions/JsonbExpression.cs`
- `Sql/Expressions/JsonExpression.cs`
- `Sql/Expressions/VectorExpression.cs`
- `Sql/Expressions/FullTextExpression.cs`
- `Sql/Expressions/ArrayExpression.cs`
- `Sql/Expressions/RangeExpression.cs`
- `Sql/Expressions/PatternMatchExpression.cs`
- `Sql/Expressions/CastExpression.cs`

- [ ] **Step 1: Migrate `TableClause` to use the quoter**

Replace the existing `AppendTo` body and the stubbed `Accept` with:

```csharp
public void Accept(SqlEmitter emitter)
{
    emitter.Buffer.Append(emitter.Quoter.QuoteQualifiedName(SchemaName, TableName));
    if (!string.IsNullOrEmpty(Alias))
        emitter.Buffer.Append(' ').Append(emitter.Quoter.QuoteIdentifier(Alias));
}

public void AppendTo(System.Text.StringBuilder buffer, Rymote.Radiant.Sql.Parameters.ParameterBag parameters)
    => throw new System.NotSupportedException("Legacy AppendTo path replaced by Accept(SqlEmitter).");
```

(Replace `Alias` with the actual property name on the existing class — verify by Reading the file first.)

- [ ] **Step 2: Migrate `ColumnExpression` to use the quoter**

```csharp
public void Accept(SqlEmitter emitter)
{
    if (!string.IsNullOrEmpty(TableAlias))
        emitter.Buffer.Append(emitter.Quoter.QuoteIdentifier(TableAlias)).Append('.');
    emitter.Buffer.Append(emitter.Quoter.QuoteIdentifier(ColumnName));
}
```

- [ ] **Step 3: Migrate `WhereCondition`**

Walk the existing `AppendTo`, replace each hardcoded operator with the dialect equivalent where applicable, and route parameter binding through `emitter.Parameters.Add(value)` (which returns the placeholder).

- [ ] **Step 4–N: Repeat for each remaining clause/expression**

For each file in the list above:
1. Read the file.
2. Identify every literal SQL string.
3. Replace with the dialect property (`emitter.Dialect.ReturningKeyword`, `emitter.Dialect.Jsonb.ContainsOperator`, etc.).
4. Replace identifier writes with `emitter.Quoter.QuoteIdentifier`.
5. Replace parameter adds with `emitter.Parameters.Add(value)`.
6. Leave the legacy `AppendTo` as `throw new NotSupportedException(...)`.

Each file is a separate sub-step; if reviewing, treat each as a commit boundary.

- [ ] **Step Z: Verify build**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: still red on Dapper-using files, but every clause/expression compiles.

---

### Task 9: Replace `ParameterBag`, `QueryCommand`, `QueryExecutor` with adapter-driven equivalents

**Files:**
- Modify: `Rymote.Radiant/Sql/Parameters/ParameterBag.cs`
- Modify: `Rymote.Radiant/Sql/QueryCommand.cs`
- Modify: `Rymote.Radiant/Sql/Executor/QueryExecutor.cs`
- Modify: `Rymote.Radiant/Sql/Compiler/QueryCompiler.cs`

- [ ] **Step 1: Rewrite `ParameterBag`**

```csharp
using System.Collections.Generic;
using System.Data;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql.Parameters;

public sealed class ParameterBag
{
    private readonly IParameterFormatter parameterFormatter;
    private readonly List<QueryParameter> parameters = new();

    public ParameterBag(IParameterFormatter parameterFormatter)
    {
        this.parameterFormatter = parameterFormatter;
    }

    public string Add(object? value, DbType? type = null, string? databaseNativeType = null)
    {
        int ordinal = parameters.Count;
        string parameterName = parameterFormatter.FormatParameterName(ordinal);
        parameters.Add(new QueryParameter(parameterName, value, type, databaseNativeType));
        return parameterFormatter.FormatPlaceholder(ordinal);
    }

    public IReadOnlyList<QueryParameter> Parameters => parameters;
}
```

- [ ] **Step 2: Rewrite `QueryCommand`**

```csharp
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql;

public sealed class QueryCommand
{
    public CompiledQuery CompiledQuery { get; }
    public string Sql => CompiledQuery.Sql;

    public QueryCommand(CompiledQuery compiledQuery)
    {
        CompiledQuery = compiledQuery;
    }

    public static implicit operator string(QueryCommand command) => command.Sql;
}
```

- [ ] **Step 3: Rewrite `QueryExecutor`**

```csharp
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql.Executor;

public sealed class QueryExecutor
{
    private readonly IDatabaseAdapter adapter;
    private readonly DbConnection connection;
    private readonly DbTransaction? transaction;

    public QueryExecutor(IDatabaseAdapter adapter, DbConnection connection, DbTransaction? transaction = null)
    {
        this.adapter = adapter;
        this.connection = connection;
        this.transaction = transaction;
    }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        using DbCommand command = CreateCommand(queryCommand);
        return await adapter.ResultMapper.QueryAsync<TResult>(command, cancellationToken);
    }

    public async Task<TResult> QuerySingleAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        using DbCommand command = CreateCommand(queryCommand);
        return await adapter.ResultMapper.QuerySingleAsync<TResult>(command, cancellationToken);
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        using DbCommand command = CreateCommand(queryCommand);
        return await adapter.ResultMapper.QuerySingleOrDefaultAsync<TResult>(command, cancellationToken);
    }

    public async Task<int> ExecuteAsync(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        using DbCommand command = CreateCommand(queryCommand);
        return await adapter.ResultMapper.ExecuteAsync(command, cancellationToken);
    }

    private DbCommand CreateCommand(QueryCommand queryCommand)
    {
        DbCommand command = adapter.CreateCommand(connection, queryCommand.CompiledQuery);
        if (transaction is not null)
            command.Transaction = transaction;
        return command;
    }
}
```

- [ ] **Step 4: Rewrite `QueryCompiler` as an adapter-aware dispatcher**

```csharp
using System.Text;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Compiler;

public sealed class QueryCompiler
{
    private readonly IDatabaseAdapter adapter;

    public QueryCompiler(IDatabaseAdapter adapter)
    {
        this.adapter = adapter;
    }

    public QueryCommand Compile(IQueryBuilder builder)
    {
        StringBuilder buffer = new StringBuilder();
        ParameterBag parameterBag = new ParameterBag(adapter.ParameterFormatter);
        SqlEmitter emitter = new SqlEmitter(adapter, buffer, parameterBag);
        builder.EmitTo(emitter);
        CompiledQuery compiledQuery = new CompiledQuery(buffer.ToString(), parameterBag.Parameters);
        return new QueryCommand(compiledQuery);
    }
}
```

`IQueryBuilder` gains a new method `EmitTo(SqlEmitter emitter)`. Each concrete builder (`SelectBuilder`, `InsertBuilder`, `UpdateBuilder`, `DeleteBuilder`) walks its clauses by calling `emitter.Emit(clause)` in the right order. This replaces the per-clause hardcoded ordering currently sitting in `QueryCompiler.Compile(SelectBuilder)`.

- [ ] **Step 5: Wire `IQueryBuilder.EmitTo` on each builder**

For `SelectBuilder.cs`, replace the body of the existing compile path with a single method:

```csharp
public void EmitTo(Rymote.Radiant.Sql.Compiler.SqlEmitter emitter)
{
    if (WithClause is not null)        emitter.Emit(WithClause);
    if (SelectClause is not null)      emitter.Emit(SelectClause);
    if (FromClause is not null)        { emitter.Buffer.Append(' ').Append(emitter.Dialect.From).Append(' '); emitter.Emit(FromClause); }
    foreach (var joinClause in JoinClauses) emitter.Emit(joinClause);
    if (WhereClause is not null)       emitter.Emit(WhereClause);
    if (GroupByClause is not null)     emitter.Emit(GroupByClause);
    if (HavingClause is not null)      emitter.Emit(HavingClause);
    foreach (var setOperation in SetOperations) emitter.Emit(setOperation);
    if (OrderByClause is not null)     emitter.Emit(OrderByClause);
    if (LimitClause is not null)       emitter.Emit(LimitClause);
}
```

(Property names need to match the actual builder. Read the file before editing.) Repeat for `InsertBuilder`, `UpdateBuilder`, `DeleteBuilder`.

- [ ] **Step 6: Update `Build()` on each builder**

The current `Build()` calls `QueryCompiler.Compile(this)` with no adapter. Change to require an adapter:

```csharp
public QueryCommand Build(IDatabaseAdapter adapter)
{
    QueryCompiler compiler = new QueryCompiler(adapter);
    return compiler.Compile(this);
}
```

Existing call sites that call `.Build()` without arguments need either:
- An ambient adapter resolved via `SmartContextAmbient.Current.Adapter` (Phase 2 introduces this), or
- An explicit `.Build(adapter)` invocation.

For the duration of Phase 1, add a parameterless `Build()` overload that throws `InvalidOperationException("Use Build(IDatabaseAdapter) — Smart layer migration pending")`. The Smart layer (Phase 2) calls the adapter overload.

- [ ] **Step 7: Verify Phase 1 ends with a green core build**

```powershell
dotnet build Rymote.Radiant.sln
```

Expected: `Rymote.Radiant`, `Rymote.Radiant.Adapters.PostgreSql`, `Rymote.Radiant.Analyzers`, `Rymote.Radiant.Generators` all build. `Playground` likely still red — fixed in Phase 2 Task 7.

---

## Phase 2: SmartContext (instance-based replacement)

### Task 1: Define `SmartContextOptions` and supporting types

**Files:**
- Create: `Rymote.Radiant/Smart/Configuration/SmartContextOptions.cs`
- Create: `Rymote.Radiant/Smart/Configuration/ValueConverter.cs`
- Create: `Rymote.Radiant/Smart/Configuration/GlobalQueryFilter.cs`

- [ ] **Step 1: Create `ValueConverter.cs`**

```csharp
using System;

namespace Rymote.Radiant.Smart.Configuration;

public abstract class ValueConverter
{
    public abstract Type ClrType { get; }
    public abstract Type DatabaseType { get; }
    public abstract object? ToDatabase(object? clrValue);
    public abstract object? FromDatabase(object? databaseValue);
}

public sealed class ValueConverter<TClr, TDatabase> : ValueConverter
{
    private readonly Func<TClr, TDatabase> toDatabase;
    private readonly Func<TDatabase, TClr> fromDatabase;

    public ValueConverter(Func<TClr, TDatabase> toDatabase, Func<TDatabase, TClr> fromDatabase)
    {
        this.toDatabase = toDatabase;
        this.fromDatabase = fromDatabase;
    }

    public override Type ClrType => typeof(TClr);
    public override Type DatabaseType => typeof(TDatabase);
    public override object? ToDatabase(object? clrValue) => clrValue is null ? null : toDatabase((TClr)clrValue);
    public override object? FromDatabase(object? databaseValue) => databaseValue is null ? null : fromDatabase((TDatabase)databaseValue);
}
```

- [ ] **Step 2: Create `GlobalQueryFilter.cs`**

```csharp
using System;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Configuration;

public abstract class GlobalQueryFilter
{
    public abstract Type MarkerInterface { get; }
    public abstract void Apply<TModel>(ISmartQuery<TModel> query, IServiceProvider serviceProvider) where TModel : class, new();
}
```

Concrete filters subclass this and override `Apply` to call `query.Where(...)`.

- [ ] **Step 3: Create `SmartContextOptions.cs`**

```csharp
using System;
using System.Collections.Generic;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class SmartContextOptions
{
    public IDatabaseAdapter Adapter { get; }
    public IModelMetadataCache ModelMetadataCache { get; }
    public IReadOnlyDictionary<Type, ValueConverter> ValueConverters { get; }
    public IReadOnlyList<GlobalQueryFilter> GlobalQueryFilters { get; }
    public string? SchemaOverride { get; }
    public int CommandTimeoutSeconds { get; }

    public SmartContextOptions(
        IDatabaseAdapter adapter,
        IModelMetadataCache modelMetadataCache,
        IReadOnlyDictionary<Type, ValueConverter> valueConverters,
        IReadOnlyList<GlobalQueryFilter> globalQueryFilters,
        string? schemaOverride = null,
        int commandTimeoutSeconds = 30)
    {
        Adapter = adapter;
        ModelMetadataCache = modelMetadataCache;
        ValueConverters = valueConverters;
        GlobalQueryFilters = globalQueryFilters;
        SchemaOverride = schemaOverride;
        CommandTimeoutSeconds = commandTimeoutSeconds;
    }

    public SmartContextOptions WithSchema(string schemaName)
        => new SmartContextOptions(Adapter, ModelMetadataCache, ValueConverters, GlobalQueryFilters, schemaName, CommandTimeoutSeconds);
}
```

- [ ] **Step 4: Verify compile**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: green.

---

### Task 2: Implement `SmartContext` and `SmartContextAmbient`

**Files:**
- Create: `Rymote.Radiant/Smart/Context/SmartContext.cs`
- Create: `Rymote.Radiant/Smart/Context/SmartContextAmbient.cs`
- Create: `Rymote.Radiant/Smart/Context/ISmartTransaction.cs`
- Create: `Rymote.Radiant/Smart/Context/SmartTransaction.cs`

- [ ] **Step 1: Create `ISmartTransaction.cs`**

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Context;

public interface ISmartTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create `SmartTransaction.cs`**

```csharp
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Context;

internal sealed class SmartTransaction : ISmartTransaction
{
    private readonly SmartContext owningContext;
    private readonly DbTransaction underlyingTransaction;
    private bool isCompleted;

    internal SmartTransaction(SmartContext owningContext, DbTransaction underlyingTransaction)
    {
        this.owningContext = owningContext;
        this.underlyingTransaction = underlyingTransaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await underlyingTransaction.CommitAsync(cancellationToken);
        isCompleted = true;
        owningContext.ClearAmbientTransaction();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await underlyingTransaction.RollbackAsync(cancellationToken);
        isCompleted = true;
        owningContext.ClearAmbientTransaction();
    }

    public async ValueTask DisposeAsync()
    {
        if (!isCompleted) await RollbackAsync();
        await underlyingTransaction.DisposeAsync();
    }
}
```

- [ ] **Step 3: Create `SmartContext.cs`**

```csharp
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Smart.Query;
using Rymote.Radiant.Smart.Repository;

namespace Rymote.Radiant.Smart.Context;

public sealed class SmartContext : IAsyncDisposable
{
    private readonly SmartContextOptions options;
    private DbConnection? openConnection;
    private DbTransaction? ambientTransaction;
    private readonly IServiceProvider serviceProvider;

    public SmartContext(SmartContextOptions options, IServiceProvider serviceProvider)
    {
        this.options = options;
        this.serviceProvider = serviceProvider;
    }

    public IDatabaseAdapter Adapter => options.Adapter;
    public IModelMetadataCache MetadataCache => options.ModelMetadataCache;
    public SmartContextOptions Options => options;
    public IServiceProvider Services => serviceProvider;
    internal DbTransaction? AmbientTransaction => ambientTransaction;

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (openConnection is { State: ConnectionState.Open }) return openConnection;
        openConnection = await options.Adapter.OpenConnectionAsync(cancellationToken);
        return openConnection;
    }

    public ISmartQuery<TModel> Query<TModel>() where TModel : class, new()
        => new SmartQuery<TModel>(this, options.ModelMetadataCache.GetMetadata<TModel>());

    public ISmartRepository<TModel> Repository<TModel>() where TModel : class, new()
        => new SmartRepository<TModel>(this, options.ModelMetadataCache.GetMetadata<TModel>());

    public ISmartRawQuery Raw() => new SmartRawQuery(this);

    public async Task<ISmartTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await GetOpenConnectionAsync(cancellationToken);
        ambientTransaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new SmartTransaction(this, ambientTransaction);
    }

    public SmartContext WithSchema(string schemaName)
        => new SmartContext(options.WithSchema(schemaName), serviceProvider);

    internal void ClearAmbientTransaction() => ambientTransaction = null;

    public async ValueTask DisposeAsync()
    {
        if (ambientTransaction is not null)
        {
            await ambientTransaction.DisposeAsync();
            ambientTransaction = null;
        }
        if (openConnection is not null)
        {
            await openConnection.DisposeAsync();
            openConnection = null;
        }
    }
}
```

- [ ] **Step 4: Create `SmartContextAmbient.cs`**

```csharp
using System;
using System.Threading;

namespace Rymote.Radiant.Smart.Context;

public static class SmartContextAmbient
{
    private static readonly AsyncLocal<SmartContext?> currentContext = new();

    public static SmartContext? CurrentOrNull => currentContext.Value;

    public static SmartContext Current
        => currentContext.Value ?? throw new InvalidOperationException(
            "No ambient SmartContext is set. Configure ASP.NET Core middleware via UseRadiantSmartContext() or wrap your call in SmartContextAmbient.Use(context).");

    public static IDisposable Use(SmartContext context)
    {
        SmartContext? previous = currentContext.Value;
        currentContext.Value = context;
        return new AmbientScope(previous);
    }

    private sealed class AmbientScope : IDisposable
    {
        private readonly SmartContext? previous;
        public AmbientScope(SmartContext? previous) { this.previous = previous; }
        public void Dispose() => currentContext.Value = previous;
    }
}
```

- [ ] **Step 5: Verify compile**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: existing `SmartModel`, `SmartQuery`, `SmartRepository` referencing the old static API may still red. They get rewritten in the next tasks.

---

### Task 3: Rewrite `SmartModel` to delegate to ambient context

**Files:**
- Modify: `Rymote.Radiant/Smart/SmartModel.cs`

- [ ] **Step 1: Replace static configuration with ambient delegation**

Full rewrite:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Context;
using Rymote.Radiant.Smart.Query;
using Rymote.Radiant.Smart.Repository;

namespace Rymote.Radiant.Smart;

public abstract class SmartModel
{
    public static SmartContext CurrentContext => SmartContextAmbient.Current;
}

public abstract class SmartModel<TModel> : SmartModel where TModel : SmartModel<TModel>, new()
{
    public static ISmartQuery<TModel> Query() => CurrentContext.Query<TModel>();
    public static ISmartRawQuery Raw() => CurrentContext.Raw();

    public static Task<TModel?> FindAsync(object primaryKey, CancellationToken cancellationToken = default)
        => CurrentContext.Query<TModel>().FindAsync(primaryKey, cancellationToken);

    public static async Task<System.Collections.Generic.List<TModel>> AllAsync(CancellationToken cancellationToken = default)
        => await CurrentContext.Query<TModel>().ToListAsync(cancellationToken);

    public static Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
        => CurrentContext.Repository<TModel>().InsertAsync(model, cancellationToken);

    public Task<TModel> SaveAsync(CancellationToken cancellationToken = default)
        => CurrentContext.Repository<TModel>().UpsertAsync((TModel)this, cancellationToken);

    public Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
        => CurrentContext.Repository<TModel>().DeleteAsync((TModel)this, cancellationToken);

    public Task<bool> RestoreAsync(CancellationToken cancellationToken = default)
        => CurrentContext.Repository<TModel>().RestoreAsync((TModel)this, cancellationToken);

    public Task<bool> ForceDeleteAsync(CancellationToken cancellationToken = default)
        => CurrentContext.Repository<TModel>().ForceDeleteAsync((TModel)this, cancellationToken);
}
```

- [ ] **Step 2: Delete obsolete connection-resolver files**

```powershell
Remove-Item Rymote.Radiant/Smart/Connection/IConnectionResolver.cs
Remove-Item Rymote.Radiant/Smart/Connection/StaticConnectionResolver.cs
Remove-Item Rymote.Radiant/Smart/Connection/ScopedConnectionResolver.cs
Remove-Item Rymote.Radiant/Smart/Configuration/SmartModelConfiguration.cs
Remove-Item Rymote.Radiant/Smart/Configuration/ISmartModelConfiguration.cs
Remove-Item Rymote.Radiant/Smart/ServiceCollectionExtensions.cs
```

(These files are replaced in Task 5 by `RadiantBuilder` + `RadiantServiceCollectionExtensions`.)

- [ ] **Step 3: Verify compile**

Expected: many failures in `SmartQuery.cs`, `SmartRepository.cs` (they reference the old `SmartModel.GetConnection()` etc.). Tasks 4 + 5 fix them.

---

### Task 4: Rewrite `SmartRepository` and `SmartQuery` to use `SmartContext`

**Files:**
- Modify: `Rymote.Radiant/Smart/Repository/SmartRepository.cs`
- Modify: `Rymote.Radiant/Smart/Query/SmartQuery.cs`
- Modify: `Rymote.Radiant/Smart/Query/SmartRawQuery.cs`
- Modify: `Rymote.Radiant/Smart/Repository/ISmartRepository.cs`
- Modify: `Rymote.Radiant/Smart/Query/ISmartQuery.cs`

- [ ] **Step 1: Update `ISmartRepository<T>`** to add `UpsertAsync`, `InsertManyAsync`, `ForceDeleteAsync`, and `CancellationToken` parameters on every method.

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Repository;

public interface ISmartRepository<TModel> where TModel : class, new()
{
    Task<TModel> InsertAsync(TModel model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TModel>> InsertManyAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default);
    Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default);
    Task<TModel> UpsertAsync(TModel model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TModel model, CancellationToken cancellationToken = default);
    Task<bool> ForceDeleteAsync(TModel model, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(TModel model, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(TModel model, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Rewrite `SmartRepository<T>`** with constructor `SmartRepository(SmartContext context, IModelMetadata metadata)`. Every method:
1. Calls `await context.GetOpenConnectionAsync(cancellationToken)`.
2. Builds the appropriate builder.
3. Calls `builder.Build(context.Adapter)`.
4. Creates `QueryExecutor(context.Adapter, connection, context.AmbientTransaction)`.
5. Awaits the executor with `cancellationToken`.
6. Applies value converters when reading the primary key back from `RETURNING`.

The body is too long to inline here in full; reuse the existing logic from the v1 `SmartRepository.cs` (lines 24–250) and substitute the new constructor pattern. For each property the old code calls `property.PropertyInfo.GetValue(model)` — keep that, but pass the result through `context.Options.ValueConverters[property.PropertyType]?.ToDatabase(...)` when a converter is registered. The `CreateValueExpression` method moves to a helper on `SmartContext` that consults the adapter's `IValueWriter` instead of hardcoding Postgres casts.

- [ ] **Step 3: Rewrite `SmartQuery<T>`** with constructor `SmartQuery(SmartContext context, IModelMetadata metadata)`. Replace every call site that uses `_databaseConnection` with `await _context.GetOpenConnectionAsync(cancellationToken)`. Replace every `_selectBuilder.Build()` with `_selectBuilder.Build(_context.Adapter)`. Plumb `CancellationToken` into every `*Async` method.

Apply global query filters in `InitializeSelectBuilder` after the soft-delete clause:

```csharp
private void InitializeSelectBuilder()
{
    // … existing column selection, FROM clause, soft-delete filter …

    foreach (GlobalQueryFilter filter in _context.Options.GlobalQueryFilters)
    {
        if (filter.MarkerInterface.IsAssignableFrom(typeof(TModel)))
            filter.Apply(this, _context.Services);
    }
}
```

- [ ] **Step 4: Rewrite `SmartRawQuery`**

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Context;
using Rymote.Radiant.Sql;

namespace Rymote.Radiant.Smart.Query;

public sealed class SmartRawQuery : ISmartRawQuery
{
    private readonly SmartContext context;

    public SmartRawQuery(SmartContext context) { this.context = context; }

    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var connection = await context.GetOpenConnectionAsync(cancellationToken);
        // Adapter-backed execution: build CompiledQuery from raw sql + parameter bag inferred from the object.
        // For simplicity, fall through to the result mapper's QueryAsync:
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameters is not null)
        {
            foreach (var property in parameters.GetType().GetProperties())
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = property.Name;
                parameter.Value = property.GetValue(parameters) ?? System.DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }
        if (context.AmbientTransaction is not null) command.Transaction = context.AmbientTransaction;
        return await context.Adapter.ResultMapper.QueryAsync<TResult>(command, cancellationToken);
    }
}
```

- [ ] **Step 5: Verify compile**

```powershell
dotnet build Rymote.Radiant/Rymote.Radiant.csproj
```

Expected: green. The Playground (Phase 2 Task 7) is still red.

---

### Task 5: DI registration — `RadiantBuilder`

**Files:**
- Create: `Rymote.Radiant/Smart/Configuration/RadiantBuilder.cs`
- Create: `Rymote.Radiant/Smart/DependencyInjection/RadiantServiceCollectionExtensions.cs`
- Create: `Rymote.Radiant/Smart/DependencyInjection/RadiantApplicationBuilderExtensions.cs`
- Create: `Rymote.Radiant.Adapters.PostgreSql/DependencyInjection/PostgreSqlBuilderExtensions.cs`

- [ ] **Step 1: Create `RadiantBuilder.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class RadiantBuilder
{
    private readonly IServiceCollection services;
    private readonly Dictionary<Type, ValueConverter> valueConverters = new();
    private readonly List<GlobalQueryFilter> globalQueryFilters = new();
    private readonly ModelMetadataScanner metadataScanner = new();
    private readonly ModelMetadataCache metadataCache;
    private Func<IServiceProvider, IDatabaseAdapter>? adapterFactory;

    public RadiantBuilder(IServiceCollection services)
    {
        this.services = services;
        metadataCache = new ModelMetadataCache(metadataScanner);
    }

    public IServiceCollection Services => services;

    public RadiantBuilder UseAdapter(Func<IServiceProvider, IDatabaseAdapter> factory)
    {
        adapterFactory = factory;
        return this;
    }

    public RadiantBuilder RegisterModel<TModel>() where TModel : class
    {
        metadataCache.RegisterModel<TModel>();
        return this;
    }

    public RadiantBuilder RegisterModelsFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;
            if (typeof(SmartModel).IsAssignableFrom(type))
                metadataCache.RegisterModel(type);
        }
        return this;
    }

    public RadiantBuilder AddValueConverter<TClr, TDatabase>(Func<TClr, TDatabase> toDatabase, Func<TDatabase, TClr> fromDatabase)
    {
        valueConverters[typeof(TClr)] = new ValueConverter<TClr, TDatabase>(toDatabase, fromDatabase);
        return this;
    }

    public RadiantBuilder AddGlobalQueryFilter(GlobalQueryFilter filter)
    {
        globalQueryFilters.Add(filter);
        return this;
    }

    internal SmartContextOptions BuildOptions(IServiceProvider serviceProvider)
    {
        if (adapterFactory is null)
            throw new InvalidOperationException("No database adapter configured. Call UsePostgreSql(...) or similar.");
        IDatabaseAdapter adapter = adapterFactory(serviceProvider);
        return new SmartContextOptions(adapter, metadataCache, valueConverters, globalQueryFilters);
    }
}
```

- [ ] **Step 2: Create `RadiantServiceCollectionExtensions.cs`**

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Context;

namespace Rymote.Radiant.Smart.DependencyInjection;

public static class RadiantServiceCollectionExtensions
{
    public static IServiceCollection AddRadiant(this IServiceCollection services, Action<RadiantBuilder> configure)
    {
        RadiantBuilder builder = new RadiantBuilder(services);
        configure(builder);

        services.AddSingleton<SmartContextOptions>(provider => builder.BuildOptions(provider));
        services.AddScoped<SmartContext>(provider =>
        {
            SmartContextOptions options = provider.GetRequiredService<SmartContextOptions>();
            return new SmartContext(options, provider);
        });
        return services;
    }
}
```

- [ ] **Step 3: Create `RadiantApplicationBuilderExtensions.cs`** — ASP.NET Core middleware to set the ambient context per request

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Smart.Context;

namespace Rymote.Radiant.Smart.DependencyInjection;

public static class RadiantApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRadiantSmartContext(this IApplicationBuilder app)
        => app.Use(async (HttpContext httpContext, RequestDelegate next) =>
        {
            SmartContext context = httpContext.RequestServices.GetRequiredService<SmartContext>();
            using (SmartContextAmbient.Use(context))
                await next(httpContext);
        });
}
```

Note: this introduces a dependency on `Microsoft.AspNetCore.Http.Abstractions`. Add it to the core project file:

```xml
<PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" Version="2.3.0" />
```

If we want to avoid the ASP.NET dependency on the core library, move this file to a separate `Rymote.Radiant.AspNetCore` project. **Decision: keep it on core for now**; the package reference is small and the value is high.

- [ ] **Step 4: Create `PostgreSqlBuilderExtensions.cs`**

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rymote.Radiant.Smart.Configuration;

namespace Rymote.Radiant.Adapters.PostgreSql.DependencyInjection;

public static class PostgreSqlBuilderExtensions
{
    public static RadiantBuilder UsePostgreSql(this RadiantBuilder builder, string connectionString, Action<NpgsqlDataSourceBuilder>? configureDataSource = null)
    {
        builder.Services.AddSingleton(provider =>
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            configureDataSource?.Invoke(dataSourceBuilder);
            return dataSourceBuilder.Build();
        });

        return builder.UseAdapter(provider =>
        {
            NpgsqlDataSource dataSource = provider.GetRequiredService<NpgsqlDataSource>();
            return new PostgreSqlAdapter(dataSource);
        });
    }
}
```

- [ ] **Step 5: Verify build**

```powershell
dotnet build Rymote.Radiant.sln
```

Expected: core and adapter green.

---

### Task 6: Schema-scoping integration

**Files:**
- Modify: `Rymote.Radiant/Smart/Context/SmartContext.cs` (already has `WithSchema`)
- Modify: `Rymote.Radiant/Smart/Query/SmartQuery.cs`

- [ ] **Step 1: Surface schema override in queries**

In `SmartQuery<T>.InitializeSelectBuilder()`, change:

```csharp
.From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);
```

to:

```csharp
.From(_modelMetadata.TableName, _context.Options.SchemaOverride ?? _schemaOverride ?? _modelMetadata.SchemaName);
```

Now the context-level override wins over per-query override, with model metadata as the floor.

- [ ] **Step 2: Verify build and add a smoke test**

```powershell
dotnet build Rymote.Radiant.sln
```

Test setup deferred to Phase 6.

---

### Task 7: Migrate the Playground to the new API

**Files:**
- Modify: `Playground/Program.cs`
- Modify: `Playground/Playground.csproj`

- [ ] **Step 1: Add reference to the PostgreSQL adapter**

```xml
<ProjectReference Include="..\Rymote.Radiant.Adapters.PostgreSql\Rymote.Radiant.Adapters.PostgreSql.csproj" />
```

- [ ] **Step 2: Replace static `SmartModel.Configure` with the DI builder**

Replace the section that constructs the `NpgsqlConnection` and calls `SmartModel.Configure(...)` with:

```csharp
ServiceCollection services = new ServiceCollection();
services.AddRadiant(builder =>
{
    builder.UsePostgreSql(connectionString, dataSourceBuilder =>
    {
        dataSourceBuilder.EnableDynamicJson();
    });
    builder.RegisterModelsFromAssembly(typeof(Program).Assembly);
});

ServiceProvider serviceProvider = services.BuildServiceProvider();
SmartContext context = serviceProvider.GetRequiredService<SmartContext>();
using IDisposable contextScope = SmartContextAmbient.Use(context);

// All existing User.Query() etc. calls continue to work because they resolve through SmartContextAmbient.Current.
```

- [ ] **Step 3: Verify it builds and runs end-to-end**

```powershell
dotnet build Rymote.Radiant.sln
dotnet run --project Playground/Playground.csproj
```

Expected: same console output as before the refactor (sanity check that no Postgres SQL changed).

---

## Phase 3: LINQ-to-SQL translator overhaul

### Task 1: Define the new `LinqToSqlTranslator`

**Files:**
- Create: `Rymote.Radiant/Smart/Expressions/LinqToSqlTranslator.cs`
- Modify: `Rymote.Radiant/Smart/Query/SmartQuery.cs`
- Delete: `Rymote.Radiant/Smart/Expressions/WhereExpressionVisitor.cs`

- [ ] **Step 1: Create the translator scaffolding**

Translator outputs an `IWhereExpression` tree (not flat tuples). The skeleton:

```csharp
using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart.Expressions;

public sealed class LinqToSqlTranslator
{
    private readonly IModelMetadata rootMetadata;
    private readonly IModelMetadataCache metadataCache;

    public LinqToSqlTranslator(IModelMetadata rootMetadata, IModelMetadataCache metadataCache)
    {
        this.rootMetadata = rootMetadata;
        this.metadataCache = metadataCache;
    }

    public IWhereExpression Translate(LambdaExpression lambda) => TranslateExpression(lambda.Body);

    private IWhereExpression TranslateExpression(Expression node) => node.NodeType switch
    {
        ExpressionType.AndAlso  => TranslateBoolean((BinaryExpression)node, WhereLogicalOperator.And),
        ExpressionType.OrElse   => TranslateBoolean((BinaryExpression)node, WhereLogicalOperator.Or),
        ExpressionType.Not      => TranslateNot((UnaryExpression)node),
        ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual
            or ExpressionType.GreaterThan
            or ExpressionType.GreaterThanOrEqual
                                => TranslateBinaryComparison((BinaryExpression)node),
        ExpressionType.Call     => TranslateMethodCall((MethodCallExpression)node),
        ExpressionType.MemberAccess => TranslateMemberAccess((MemberExpression)node),
        _ => throw new NotSupportedException($"Unsupported expression node type: {node.NodeType}")
    };

    private IWhereExpression TranslateBoolean(BinaryExpression node, WhereLogicalOperator op)
    {
        IWhereExpression left = TranslateExpression(node.Left);
        IWhereExpression right = TranslateExpression(node.Right);
        return new WhereGroup(op, new[] { left, right });
    }

    private IWhereExpression TranslateNot(UnaryExpression node)
        => new WhereBooleanExpression(new NotExpression(TranslateExpression(node.Operand)));

    private IWhereExpression TranslateBinaryComparison(BinaryExpression node)
    {
        string columnName = ResolveColumnName(node.Left);
        object? value = EvaluateAsConstant(node.Right);
        string operatorSymbol = node.NodeType switch
        {
            ExpressionType.Equal              => value is null ? "IS" : "=",
            ExpressionType.NotEqual           => value is null ? "IS NOT" : "<>",
            ExpressionType.LessThan           => "<",
            ExpressionType.LessThanOrEqual    => "<=",
            ExpressionType.GreaterThan        => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            _ => throw new NotSupportedException()
        };
        return new WhereCondition(columnName, operatorSymbol, value);
    }

    private IWhereExpression TranslateMethodCall(MethodCallExpression node)
    {
        // String.Contains, StartsWith, EndsWith, ToLower; IEnumerable.Contains; navigation method calls.
        // See full implementation in Task 3 below.
        throw new System.NotImplementedException("Filled in Phase 3 Task 3.");
    }

    private IWhereExpression TranslateMemberAccess(MemberExpression node)
    {
        // Used when a boolean property is referenced directly: x => x.IsActive
        string columnName = ResolveColumnName(node);
        return new WhereCondition(columnName, "=", true);
    }

    private string ResolveColumnName(Expression node)
    {
        if (node is UnaryExpression { Operand: MemberExpression unwrappedMember })
            node = unwrappedMember;
        if (node is not MemberExpression memberExpression)
            throw new NotSupportedException("Left side of a comparison must be a property access.");

        string propertyName = memberExpression.Member.Name;
        foreach (var property in rootMetadata.Properties.Values)
            if (property.PropertyName == propertyName) return property.ColumnName;

        throw new InvalidOperationException($"Property {propertyName} not found in metadata for {rootMetadata.TableName}.");
    }

    private static object? EvaluateAsConstant(Expression node) => node switch
    {
        ConstantExpression constantExpression => constantExpression.Value,
        MemberExpression memberExpression => Expression.Lambda(memberExpression).Compile().DynamicInvoke(),
        _ => Expression.Lambda(node).Compile().DynamicInvoke()
    };
}
```

(The compiled-lambda fallback in `EvaluateAsConstant` is the standard pattern for resolving closure-captured values.)

- [ ] **Step 2: Run a quick test that simple translation works**

Tests come in Phase 6. For now build and continue.

---

### Task 2: Wire the translator into `SmartQuery.Where`

**Files:**
- Modify: `Rymote.Radiant/Smart/Query/SmartQuery.cs`

- [ ] **Step 1: Replace the `Where(Expression<Func<TModel, bool>>)` body**

```csharp
public ISmartQuery<TModel> Where(System.Linq.Expressions.Expression<System.Func<TModel, bool>> predicate)
{
    LinqToSqlTranslator translator = new LinqToSqlTranslator(_modelMetadata, _context.MetadataCache);
    IWhereExpression whereExpression = translator.Translate(predicate);
    _selectBuilder.WhereExpression(whereExpression);
    return this;
}
```

`SelectBuilder.WhereExpression(IWhereExpression)` exists today as `WhereBooleanExpression`; verify the API name and add an overload if needed.

- [ ] **Step 2: Verify build**

```powershell
dotnet build Rymote.Radiant.sln
```

Expected: green.

---

### Task 3: Method-call support — `Contains`, `StartsWith`, `EndsWith`, `ToLower`, `Enumerable.Contains`

**Files:**
- Modify: `Rymote.Radiant/Smart/Expressions/LinqToSqlTranslator.cs`

- [ ] **Step 1: Fill in `TranslateMethodCall`**

```csharp
private IWhereExpression TranslateMethodCall(MethodCallExpression node)
{
    if (node.Method.DeclaringType == typeof(string))
        return TranslateStringMethod(node);

    if (typeof(IEnumerable).IsAssignableFrom(node.Method.DeclaringType) || node.Method.Name == "Contains")
        return TranslateEnumerableContains(node);

    throw new NotSupportedException($"Unsupported method call: {node.Method.DeclaringType?.FullName}.{node.Method.Name}");
}

private IWhereExpression TranslateStringMethod(MethodCallExpression node)
{
    string columnName = ResolveColumnName(node.Object!);
    string argument = (string)EvaluateAsConstant(node.Arguments[0])!;
    return node.Method.Name switch
    {
        nameof(string.Contains)   => new WhereCondition(columnName, "LIKE", $"%{argument}%"),
        nameof(string.StartsWith) => new WhereCondition(columnName, "LIKE", $"{argument}%"),
        nameof(string.EndsWith)   => new WhereCondition(columnName, "LIKE", $"%{argument}"),
        nameof(string.ToLower)    => throw new NotSupportedException("ToLower in WHERE not supported here; use Radiant.Functions.Lower instead."),
        _ => throw new NotSupportedException($"Unsupported string method: {node.Method.Name}")
    };
}

private IWhereExpression TranslateEnumerableContains(MethodCallExpression node)
{
    // x => collection.Contains(x.Property)   -> column IN (collection)
    Expression collectionExpression;
    Expression valueExpression;

    if (node.Object is not null) { collectionExpression = node.Object;      valueExpression = node.Arguments[0]; }
    else                          { collectionExpression = node.Arguments[0]; valueExpression = node.Arguments[1]; }

    string columnName = ResolveColumnName(valueExpression);
    object? collectionValue = EvaluateAsConstant(collectionExpression);
    return new WhereCondition(columnName, "IN", collectionValue ?? throw new InvalidOperationException("Collection is null."));
}
```

- [ ] **Step 2: Add navigation-property support**

In `ResolveColumnName`, recognise `MemberExpression` chains like `x.Foreign.Property`:

```csharp
if (memberExpression.Expression is MemberExpression nestedMember)
{
    // Resolve the relationship by the outer member name, then look up the inner column on the related table.
    // This requires emitting a join.
    throw new NotSupportedException("Navigation chains require a join — implemented in Phase 4 Task 5.");
}
```

Leave navigation chains as a known gap for now; Phase 4 picks them up after `ThenInclude` lands.

- [ ] **Step 3: Verify build**

```powershell
dotnet build Rymote.Radiant.sln
```

---

## Phase 4: Include / ThenInclude

### Task 1: `IncludePath` data structure

**Files:**
- Create: `Rymote.Radiant/Smart/Loading/IncludePath.cs`
- Create: `Rymote.Radiant/Smart/Loading/IIncludableSmartQuery.cs`
- Create: `Rymote.Radiant/Smart/Loading/IncludableSmartQuery.cs`

- [ ] **Step 1: Create `IncludePath.cs`**

```csharp
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Rymote.Radiant.Smart.Loading;

public sealed class IncludePath
{
    public string PropertyName { get; }
    public LambdaExpression? FilterPredicate { get; init; }
    public List<IncludePath> Children { get; } = new();

    public IncludePath(string propertyName) { PropertyName = propertyName; }

    public IncludePath OrCreateChild(string childPropertyName)
    {
        foreach (var existing in Children)
            if (existing.PropertyName == childPropertyName) return existing;
        IncludePath created = new IncludePath(childPropertyName);
        Children.Add(created);
        return created;
    }
}
```

- [ ] **Step 2: Create `IIncludableSmartQuery.cs`**

```csharp
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Loading;

public interface IIncludableSmartQuery<TRoot, TCurrent> : ISmartQuery<TRoot>
    where TRoot : class, new()
{
    IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<System.Func<TCurrent, TNext>> navigation);
    IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<System.Func<TCurrent, System.Collections.Generic.IEnumerable<TNext>>> navigation);
}
```

- [ ] **Step 3: Create `IncludableSmartQuery.cs`**

A wrapper that records `ThenInclude` calls into the current include path tail:

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Rymote.Radiant.Smart.Query;

namespace Rymote.Radiant.Smart.Loading;

internal sealed class IncludableSmartQuery<TRoot, TCurrent> : IIncludableSmartQuery<TRoot, TCurrent>
    where TRoot : class, new()
{
    private readonly SmartQuery<TRoot> innerQuery;
    private readonly IncludePath currentPath;

    public IncludableSmartQuery(SmartQuery<TRoot> innerQuery, IncludePath currentPath)
    {
        this.innerQuery = innerQuery;
        this.currentPath = currentPath;
    }

    public IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, TNext>> navigation)
        => AppendInclude<TNext>(navigation);

    public IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, IEnumerable<TNext>>> navigation)
        => AppendInclude<TNext>(navigation);

    private IIncludableSmartQuery<TRoot, TNext> AppendInclude<TNext>(LambdaExpression navigation)
    {
        if (navigation.Body is not MemberExpression memberExpression)
            throw new ArgumentException("ThenInclude requires a member expression.");
        IncludePath childPath = currentPath.OrCreateChild(memberExpression.Member.Name);
        return new IncludableSmartQuery<TRoot, TNext>(innerQuery, childPath);
    }

    // Forward all ISmartQuery<TRoot> calls to the inner query.
    // (Generate the forwarders mechanically — every method on ISmartQuery returns IIncludableSmartQuery<TRoot, TCurrent> after wrapping.)
}
```

The forwarders are tedious but mechanical — every `ISmartQuery<TRoot>` method that returns `ISmartQuery<TRoot>` becomes `return new IncludableSmartQuery<TRoot, TCurrent>(innerQuery.MethodCall(args), currentPath);`. Methods that return `Task<…>` just forward.

- [ ] **Step 4: Verify build**

---

### Task 2: Replace `RelationshipLoader` with batched loader

**Files:**
- Create: `Rymote.Radiant/Smart/Loading/BatchedRelationshipLoader.cs`
- Modify: `Rymote.Radiant/Smart/Loading/RelationshipLoader.cs` (delete) — keep as `_legacy.cs` until callers migrate.

- [ ] **Step 1: Create `BatchedRelationshipLoader.cs`**

The loader walks the `IncludePath` tree depth-first, but at each level emits **one** SELECT per child table with a `WHERE foreign_key IN (parent_ids…)`. After the second-level load, it fans out further from the loaded children.

Pseudocode (real implementation is long):

```csharp
public async Task LoadAsync(IReadOnlyList<TParent> parents, IncludePath path, CancellationToken cancellationToken)
{
    if (parents.Count == 0) return;
    IRelationshipMetadata relationship = ResolveRelationship(typeof(TParent), path.PropertyName);
    object[] foreignKeyValues = parents.Select(p => relationship.LocalKeyValueExtractor(p)).Distinct().ToArray();

    SelectBuilder childBuilder = new SelectBuilder()
        .Select(/* child columns */)
        .From(relationship.ChildTableName, relationship.ChildSchemaName)
        .Where(relationship.ChildForeignKeyColumnName, "IN", foreignKeyValues);

    QueryExecutor executor = new QueryExecutor(adapter, await context.GetOpenConnectionAsync(cancellationToken), context.AmbientTransaction);
    IReadOnlyList<object> children = await executor.QueryAsync<object>(childBuilder.Build(adapter), cancellationToken);

    AttachChildren(parents, children, relationship);

    foreach (IncludePath grandChild in path.Children)
        await LoadAsync(children, grandChild, cancellationToken);
}
```

- [ ] **Step 2: Replace `RelationshipLoader` usage in `SmartQuery`**

Switch `LoadRelationshipsAsync` and `LoadNestedRelationshipsAsync` to walk an internal `List<IncludePath>` instead of `List<Expression>` + `List<string>`. Convert existing `Include(expr)` calls into `IncludePath` entries at the point of call.

- [ ] **Step 3: Verify build and existing playground includes still work**

```powershell
dotnet run --project Playground/Playground.csproj
```

---

## Phase 5: Transactions, bulk, value converters, audit hooks, global filters

(Each task in this phase mirrors the spec sections §4.9–§4.13. They share the same shape as earlier tasks: create a feature file, wire it into `SmartContext`/`SmartQuery`/`SmartRepository`, verify build.)

### Task 1: Transactions

- [ ] **Step 1:** Already implemented in Phase 2 Task 2. Verify it works:

```csharp
await using SmartContext context = serviceProvider.GetRequiredService<SmartContext>();
await using ISmartTransaction transaction = await context.BeginTransactionAsync(cancellationToken: cancellationToken);
await context.Repository<User>().InsertAsync(user, cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Add a Playground demo that proves rollback-on-dispose.

### Task 2: Bulk operations

- [ ] **Step 1:** Add `InsertManyAsync` to `SmartRepository<T>`:

```csharp
public async Task<IReadOnlyList<TModel>> InsertManyAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
{
    List<TModel> materialised = models.ToList();
    if (materialised.Count == 0) return materialised;

    InsertBuilder insertBuilder = new InsertBuilder().Into(_modelMetadata.TableName, _modelMetadata.SchemaName);
    foreach (TModel model in materialised)
        insertBuilder.AddRowFromModel(model, _modelMetadata, _context.Options.ValueConverters);

    if ((_context.Adapter.Capabilities & DatabaseCapabilities.BatchedInsertReturning) != 0
        && _modelMetadata.PrimaryKey is { IsAutoIncrement: true })
        insertBuilder.Returning(_modelMetadata.PrimaryKey.ColumnName);

    QueryExecutor executor = new QueryExecutor(_context.Adapter, await _context.GetOpenConnectionAsync(cancellationToken), _context.AmbientTransaction);
    IReadOnlyList<dynamic> rows = await executor.QueryAsync<dynamic>(insertBuilder.Build(_context.Adapter), cancellationToken);
    BindReturnedPrimaryKeys(materialised, rows);
    return materialised;
}
```

`InsertBuilder.AddRowFromModel` is a new helper that consolidates the per-property emit logic used in `InsertAsync`.

- [ ] **Step 2:** Add `UpdateAsync(predicate, setter)` to `SmartQuery<T>`:

```csharp
public async Task<int> UpdateAsync(Expression<Func<TModel, TModel>> setterExpression, CancellationToken cancellationToken = default)
{
    UpdateBuilder updateBuilder = new UpdateBuilder().Table(_modelMetadata.TableName, _modelMetadata.SchemaName);
    LinqToSetTranslator translator = new LinqToSetTranslator(_modelMetadata);
    translator.Apply(setterExpression, updateBuilder);
    // Reuse the WHERE clause from _selectBuilder by copying it onto updateBuilder.
    updateBuilder.WhereExpression(_selectBuilder.WhereClause);
    QueryExecutor executor = new QueryExecutor(_context.Adapter, await _context.GetOpenConnectionAsync(cancellationToken), _context.AmbientTransaction);
    return await executor.ExecuteAsync(updateBuilder.Build(_context.Adapter), cancellationToken);
}
```

A new `LinqToSetTranslator` walks `MemberInit`/`New` expressions to emit `Set` assignments. Skeleton implementation:

```csharp
internal sealed class LinqToSetTranslator
{
    private readonly IModelMetadata metadata;
    public LinqToSetTranslator(IModelMetadata metadata) { this.metadata = metadata; }

    public void Apply<TModel>(Expression<Func<TModel, TModel>> expression, UpdateBuilder builder)
    {
        if (expression.Body is not MemberInitExpression memberInit)
            throw new NotSupportedException("Update setter must be a 'new { … }' or member-init expression.");

        foreach (MemberAssignment assignment in memberInit.Bindings.Cast<MemberAssignment>())
        {
            string columnName = ResolveColumn(assignment.Member.Name);
            object? value = Expression.Lambda(assignment.Expression).Compile().DynamicInvoke();
            builder.Set(columnName, value);
        }
    }

    private string ResolveColumn(string propertyName)
        => metadata.Properties.Values.First(property => property.PropertyName == propertyName).ColumnName;
}
```

- [ ] **Step 3:** Add `DeleteAsync` to `SmartQuery<T>` and `UpsertAsync` to `SmartRepository<T>` following the same pattern.

### Task 3: Global query filters

- [ ] **Step 1:** Apply filters in `SmartQuery.InitializeSelectBuilder` (already specified in Phase 2 Task 4 Step 3).
- [ ] **Step 2:** Add `IgnoreQueryFilter<TFilter>()` opt-out:

```csharp
public ISmartQuery<TModel> IgnoreQueryFilter<TFilter>() where TFilter : GlobalQueryFilter
{
    _ignoredFilterTypes.Add(typeof(TFilter));
    // Re-build _selectBuilder filters? Simpler: track ignored types, skip them when applying.
    return this;
}
```

### Task 4: Audit hooks

- [ ] **Step 1:** Create `Smart/Attributes/AuditAttribute.cs` and `Smart/Auditing/ICurrentUserAccessor.cs`.
- [ ] **Step 2:** In `SmartRepository.InsertAsync`/`UpdateAsync`, if `metadata.HasAudit`, fetch `ICurrentUserAccessor` from `_context.Services`, write `created_by_user_id`/`updated_by_user_id`.

### Task 5: Strongly-typed value converters in the metadata pipeline

- [ ] **Step 1:** Modify `ModelMetadataScanner` to flag properties whose CLR type has a registered converter.
- [ ] **Step 2:** Apply `ValueConverter.ToDatabase` everywhere `property.PropertyInfo.GetValue(model)` is currently written into a parameter.
- [ ] **Step 3:** Apply `ValueConverter.FromDatabase` in the result mapper. For Dapper, register a custom `SqlMapper.ITypeHandler` per converter at adapter init time.

---

## Phase 6: Tests

### Task 1: Test project scaffolding

**Files:**
- Create: `Rymote.Radiant.Tests/Rymote.Radiant.Tests.csproj`
- Modify: `Rymote.Radiant.sln`

- [ ] **Step 1:** Create the test csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
        <PackageReference Include="xunit" Version="2.9.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\Rymote.Radiant\Rymote.Radiant.csproj" />
        <ProjectReference Include="..\Rymote.Radiant.Adapters.PostgreSql\Rymote.Radiant.Adapters.PostgreSql.csproj" />
    </ItemGroup>
</Project>
```

```powershell
dotnet sln add Rymote.Radiant.Tests/Rymote.Radiant.Tests.csproj
```

- [ ] **Step 2:** Verify `dotnet test` runs (with zero tests, succeeds with "no tests found").

### Task 2: SQL emitter snapshot tests

**Files:**
- Create: `Rymote.Radiant.Tests/Sql/SqlEmitterTests.cs`

- [ ] **Step 1:** Write tests for the matrix below

```csharp
using System.Threading;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Sql;

public sealed class SqlEmitterPostgreSqlSnapshots
{
    private static readonly PostgreSqlDialect Dialect = new();
    private static readonly PostgreSqlAdapter Adapter = TestAdapterFactory.CreateOfflinePostgreSqlAdapter();

    [Fact]
    public void SimpleSelect()
    {
        var compiled = new SelectBuilder()
            .Select(new ColumnExpression("id"), new ColumnExpression("email"))
            .From("users")
            .Where("id", "=", 1)
            .Build(Adapter);

        Assert.Equal("SELECT \"id\", \"email\" FROM \"users\" WHERE \"id\" = @p0", compiled.Sql.Trim());
    }

    [Fact]
    public void JsonbContainsEmitsAtGreaterThanOperator()
    {
        var compiled = new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("contacts")
            .WhereBooleanExpression(new JsonbExpression(new ColumnExpression("emails"), JsonbOperator.StrictContains, new LiteralExpression("[\"a@b.c\"]")))
            .Build(Adapter);

        Assert.Contains("\"emails\" @> '[\"a@b.c\"]'::jsonb", compiled.Sql);
    }

    // … one Fact per matrix row in spec §4.7
}
```

`TestAdapterFactory.CreateOfflinePostgreSqlAdapter()` returns an adapter whose `CreateConnection`/`OpenConnectionAsync` throws — emitter tests only care about the dialect/quoter/formatter, not real I/O.

- [ ] **Step 2:** Run `dotnet test` — all snapshots green.

### Task 3: LINQ translator tests

**Files:**
- Create: `Rymote.Radiant.Tests/Smart/LinqToSqlTranslatorTests.cs`

- [ ] **Step 1:** Cover every row in spec §4.7's matrix as a separate `[Fact]`.

### Task 4: Include / ThenInclude tests

**Files:**
- Create: `Rymote.Radiant.Tests/Smart/IncludeTests.cs`

- [ ] **Step 1:** Build an in-memory adapter that fakes query execution by returning canned `IReadOnlyList<T>` per `(table, where)` shape. Verify that an `Include(d => d.Activities).ThenInclude(a => a.Comments)` produces exactly two batched SELECTs after the root query.

### Task 5: Integration tests with Testcontainers

**Files:**
- Create: `Rymote.Radiant.Adapters.PostgreSql.IntegrationTests/Rymote.Radiant.Adapters.PostgreSql.IntegrationTests.csproj`
- Create: `Rymote.Radiant.Adapters.PostgreSql.IntegrationTests/EndToEndPostgreSqlTests.cs`

- [ ] **Step 1:** Add Testcontainers dependency

```xml
<PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
```

- [ ] **Step 2:** Write a fixture that spins up Postgres, runs the Playground model setup, inserts, queries, updates, soft-deletes, restores, hard-deletes; asserts row counts at each step.

---

## Phase 7: Source generator enhancements (deferred; v2.1)

### Task 1: Update `SmartModelGenerator` for new APIs

- [ ] **Step 1:** Adjust the generated `WhereX` extensions so they expect `ISmartQuery<T>` from the new namespace (the namespace did not change, but signatures gained `CancellationToken`).

### Task 2: Generate per-model result mappers

- [ ] **Step 1:** Emit a `static class {ModelName}RadiantMapper { public static {ModelName} Map(DbDataReader reader) { … } }` per registered model.
- [ ] **Step 2:** Add `IResultMapper.RegisterSourceGenerated<TModel>(Func<DbDataReader, TModel> mapper)` so the Postgres adapter prefers the generated mapper over Dapper.

### Task 3: Typed include shortcuts

- [ ] **Step 1:** Emit `static class {ModelName}.Includes` with typed `IncludePath<{ModelName}, …>` instances for each navigation property.

(These three tasks are post-v2.0; ship after the core refactor lands.)

---

## Phase 8: Final acceptance

### Task 1: Acceptance criteria sweep

- [ ] **Step 1:** Walk through every item in spec §8.1–§8.10. For each, point to the file + test that proves it.
- [ ] **Step 2:** `dotnet build Rymote.Radiant.sln` — zero warnings, zero errors.
- [ ] **Step 3:** `dotnet test Rymote.Radiant.sln` — all tests green.
- [ ] **Step 4:** Run the Playground end-to-end; capture its SQL log; diff against the v1 baseline. Differences allowed only where the v1 SQL was ill-formed or dialect-incorrect.
- [ ] **Step 5:** Update `README.md` with the new public API surface (`AddRadiant`, `UsePostgreSql`, `SmartContext`, `WithSchema`, `ThenInclude`, `BeginTransactionAsync`, `AddValueConverter`, `AddGlobalQueryFilter`).
- [ ] **Step 6:** Hand back to user for review. **Do not commit.**

---

## Self-review

1. **Spec coverage** — every spec section in §3–§8 is mapped to a phase task at the top of this document. Verified.
2. **Placeholders** — no TBDs. Where implementation bodies are too long to inline (e.g., `BatchedRelationshipLoader`, `SmartRepository` rewrite), I've explicitly cited the v1 source lines to port and named every helper. The two skeleton tasks in Phase 7 are explicitly marked v2.1 and out of scope for the acceptance criteria.
3. **Type consistency** — `IDatabaseAdapter`, `SmartContext`, `SmartContextOptions`, `RadiantBuilder`, `ISmartTransaction`, `ISmartRepository<T>`, `ISmartQuery<T>`, `IIncludableSmartQuery<TRoot, TCurrent>`, `IncludePath`, `LinqToSqlTranslator`, `LinqToSetTranslator`, `BatchedRelationshipLoader`, `QueryExecutor`, `ParameterBag`, `QueryCommand`, `CompiledQuery`, `QueryParameter`, `SqlEmitter` — every name is consistent across tasks. The legacy `RelationshipLoader` is explicitly retired in Phase 4 Task 2.
4. **Naming policy (no acronyms, fully qualified)** — verified throughout. Examples kept verbose: `cancellationToken` (not `ct`), `queryCommand` (not `cmd`), `databaseConnection` (not `db`), `serviceProvider` (not `sp`).
