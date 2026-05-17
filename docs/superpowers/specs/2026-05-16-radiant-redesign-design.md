# Rymote.Radiant Redesign — Adapter Architecture and SmartModel Overhaul

**Status:** Draft
**Date:** 2026-05-16
**Author:** Jovan Ivanovic (with Claude)
**Target consumer:** `rymote-aether-server` (currently on EF Core 10 + Dapper hybrid)

---

## 1. Goals

1. Re-architect Rymote.Radiant so that **PostgreSQL is one database adapter among many**, not the entire backend. The core library must be dialect-agnostic.
2. Make `SmartModel` a real production-grade ORM, capable of replacing the EF Core + Dapper hybrid used in `rymote-aether-server`.
3. Improve the public API: instance-based context, async + `CancellationToken` everywhere, strongly-typed predicates, navigation chains, ambient transactions.
4. Preserve every PostgreSQL capability currently shipped (JSONB, arrays, vectors, full-text, CTEs, lateral joins, RETURNING, ON CONFLICT, window functions, range types).
5. No breaking changes to the on-disk SQL output for existing PostgreSQL callers — same SQL, cleaner library.

## 2. Non-goals (out of scope for v2.0)

- Database migrations / schema management. Aether keeps EF Core migrations.
- Reverse engineering / scaffolding from existing databases.
- Change tracking comparable to EF Core's identity map. SmartModel stays stateless per-query; callers explicitly `SaveAsync()`.
- Compiled query caching as a first-class feature (deferred to v2.1).

---

## 3. Current architecture (what we have)

```
Rymote.Radiant.csproj  (single project)
├── Sql/
│   ├── Builder/        SelectBuilder, InsertBuilder, UpdateBuilder, DeleteBuilder
│   ├── Clauses/        IQueryClause and all clause types
│   ├── Expressions/    ISqlExpression + concrete types (Jsonb, Vector, FullText, Array, …)
│   ├── Compiler/       QueryCompiler — monolithic static class
│   ├── Executor/       QueryExecutor — Dapper wrapper, IDbConnection only
│   ├── Parameters/     ParameterBag — hardcoded "@p0" naming
│   ├── Dialects/       SqlKeywords — flat string constants, Postgres-only
│   └── Validation/
└── Smart/
    ├── Attributes/     [Table] [Column] [PrimaryKey] [ForeignKey] [BelongsTo] [HasOne] [HasMany] [Index] [SoftDelete] [Timestamps]
    ├── Configuration/  SmartModelConfiguration — calls SmartModel.Configure() statically
    ├── Connection/     IConnectionResolver — Static + Scoped variants
    ├── Expressions/    WhereExpressionVisitor (binary only), JoinExpressionVisitor
    ├── Loading/        RelationshipLoader — N+1 per relationship
    ├── Metadata/       ModelMetadata, ModelMetadataCache, ModelMetadataScanner
    ├── Query/          SmartQuery, SmartRawQuery
    ├── Repository/     SmartRepository — Insert/Update/Delete/SoftDelete/Restore
    └── SmartModel.cs   abstract base with **static** Configure/Query/Find/Create/Save/Delete
```

Key problems:

- Hardcoded Postgres dialect (parameter naming `@p0`, double-quote identifier quoting, `RETURNING`, `ON CONFLICT`, `LATERAL`, JSONB/vector/array operators).
- `Npgsql 10.0.2` is a direct dependency in the core library.
- `SmartModel.Configure()` is **static** — one configuration per AppDomain. Multi-tenant (per-request schema) is grafted on via `ScopedConnectionResolver` but the model metadata cache is still static, so model registration is global.
- `WhereExpressionVisitor` only supports binary comparisons (`==`, `!=`, `<`, `>`, `<=`, `>=`). No `&&`, `||`, `!`, `Contains`, `StartsWith`, `EndsWith`, navigation chains, method calls, nested predicates.
- No `CancellationToken` anywhere in the async API.
- No transactions. `SmartRepository` opens an executor per call.
- No `ThenInclude`. Aether needs deep navigation chains like `Deal → ContactRealEstateLink → Contact → Supervisor → User`.
- No strongly-typed ID / value-converter pipeline. Aether uses StronglyTypedIds heavily.
- No global query filters (tenant scoping, archived rows, etc.).
- `RelationshipLoader` does one SELECT per `Include` per result set — N+1 batches, not JOIN-eager.

---

## 4. Target architecture

### 4.1 Project layout

```
Rymote.Radiant.sln
├── Rymote.Radiant/                          ← core: dialect-agnostic SQL + Smart ORM
├── Rymote.Radiant.Adapters.PostgreSql/      ← PostgreSQL dialect + Npgsql wiring
├── Rymote.Radiant.Adapters.SqlServer/       ← (skeleton for v2.0, full in v2.1)
├── Rymote.Radiant.Adapters.MySql/           ← (skeleton)
├── Rymote.Radiant.Adapters.Sqlite/          ← (skeleton)
├── Rymote.Radiant.Analyzers/
├── Rymote.Radiant.Generators/
├── Rymote.Radiant.Testing/                  ← in-memory adapter for unit tests
└── Playground/
```

The core `Rymote.Radiant` keeps the `Sql/*` and `Smart/*` namespaces but loses all dialect-specific hardcoding. **`Npgsql` and `Dapper` move out of the core project's direct dependencies.** Core depends only on `System.Data.Common` and `Microsoft.Extensions.DependencyInjection.Abstractions`. Dapper remains an adapter implementation detail.

### 4.2 The `IDatabaseAdapter` contract

The single hand-off point between dialect-agnostic core and dialect-specific implementation:

```csharp
namespace Rymote.Radiant.Adapters;

public interface IDatabaseAdapter
{
    string Identifier { get; }                          // "postgresql", "sqlserver", "mysql", "sqlite"
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

Component responsibilities:

| Component             | Owns                                                                                                  |
| --------------------- | ----------------------------------------------------------------------------------------------------- |
| `ISqlDialect`         | All SQL keyword/operator strings, function names. The old `SqlKeywords` becomes the Postgres dialect. |
| `IIdentifierQuoter`   | Quote table/column/schema names. Postgres uses `"`, SqlServer uses `[]`, MySql uses backtick.         |
| `IParameterFormatter` | Render parameter placeholders. Postgres `$1, $2`; SqlServer `@p0, @p1`; MySql/Sqlite `?` or `@p0`.    |
| `IValueWriter`        | Render type-cast literals (`'{...}'::jsonb`, `ARRAY[1,2,3]::int[]`, `'[0.1,0.2]'::vector`).           |
| `IResultMapper`       | Map a `DbDataReader` row to a CLR object. Default uses Dapper; alternative uses source-gen mappers.   |

`DatabaseCapabilities` is a flags enum:

```csharp
[Flags]
public enum DatabaseCapabilities : long
{
    None                  = 0,
    ReturningClause       = 1L << 0,
    UpsertOnConflict      = 1L << 1,
    UpsertMerge           = 1L << 2,
    CommonTableExpression = 1L << 3,
    RecursiveCte          = 1L << 4,
    LateralJoin           = 1L << 5,
    WindowFunctions       = 1L << 6,
    SchemaPerTable        = 1L << 7,
    JsonbColumn           = 1L << 8,
    ArrayColumn           = 1L << 9,
    VectorColumn          = 1L << 10,
    FullTextSearch        = 1L << 11,
    RangeTypes            = 1L << 12,
    Citext                = 1L << 13,
    SpatialTypes          = 1L << 14,
    IlikeOperator         = 1L << 15,
    RegexOperator         = 1L << 16,
    NamedSequences        = 1L << 17,
    BatchedInsertReturning= 1L << 18,
}
```

Clauses and expressions that require a capability **opt in via the dialect**, not via direct checks. Example:

```csharp
public interface ISqlDialect
{
    void WriteSoftDeleteRestore(StringBuilder sql, string columnName);
    string CaseInsensitiveLikeOperator { get; }          // "ILIKE" on Postgres, "LIKE" + collation on others
    string CurrentTimestampExpression { get; }
    string Now();
    string ConcatenateOperator { get; }                  // "||" Postgres, "+" SqlServer
    string ReturningKeyword { get; }                     // "RETURNING" Postgres, "OUTPUT" SqlServer (different placement)
    JsonbDialect Jsonb { get; }
    ArrayDialect Array { get; }
    VectorDialect Vector { get; }
    FullTextDialect FullText { get; }
    bool TryRenderUpsert(InsertClause insert, OnConflictClause onConflict, StringBuilder sql, ParameterBag parameters);
    // …
}
```

Expressions that don't translate (e.g. `VectorExpression` on SqlServer) throw `UnsupportedQueryOperationException` from the **adapter**, not from a hardcoded check in the core builder. The builder still accepts them; the compiler emits them through the dialect.

### 4.3 QueryCompiler refactor

`QueryCompiler` becomes a thin dispatcher; the visitor pattern moves into a `SqlEmitter` that walks clauses and expressions:

```csharp
public sealed class SqlEmitter
{
    private readonly ISqlDialect dialect;
    private readonly IIdentifierQuoter quoter;
    private readonly IParameterFormatter parameterFormatter;
    private readonly IValueWriter valueWriter;
    private readonly ParameterBag parameterBag;
    private readonly StringBuilder buffer;
    // … visit methods per IQueryClause subtype, per ISqlExpression subtype
}
```

`IQueryClause.AppendTo(StringBuilder, ParameterBag)` becomes `Accept(SqlEmitter)`. Same for `ISqlExpression`. The core no longer touches raw `"` or `$1` strings.

### 4.4 ParameterBag refactor

```csharp
public sealed class ParameterBag
{
    private readonly List<QueryParameter> parameters = new();
    public string Add(object? value, DbType? explicitType = null);
    public IReadOnlyList<QueryParameter> Parameters => parameters;
}

public sealed record QueryParameter(string Name, object? Value, DbType? Type);
```

`Add()` returns the **placeholder string already rendered for the active dialect** (e.g. `$1` or `@p1`). The adapter's `IParameterFormatter` decides naming; the bag tracks order and types.

The PostgreSQL adapter binds parameters by name to `NpgsqlParameter` instances at command-build time; Dapper integration stays optional via `IResultMapper`.

### 4.5 SmartContext — instance-based replacement for static SmartModel

The biggest API change. `SmartModel` keeps a `SmartModel`/`SmartModel<T>` base class for ergonomics, but the static configuration goes away. All operations route through a `SmartContext`:

```csharp
public sealed class SmartContext : IAsyncDisposable
{
    public SmartContext(IDatabaseAdapter adapter, SmartContextOptions options);

    public ISmartQuery<TModel> Query<TModel>() where TModel : SmartModel<TModel>, new();
    public ISmartRepository<TModel> Repository<TModel>() where TModel : SmartModel<TModel>, new();
    public ISmartRawQuery Raw();

    public Task<TModel?> FindAsync<TModel, TKey>(TKey primaryKey, CancellationToken cancellationToken = default);
    public Task<TModel> InsertAsync<TModel>(TModel model, CancellationToken cancellationToken = default);
    public Task<TModel> UpdateAsync<TModel>(TModel model, CancellationToken cancellationToken = default);
    public Task<bool> DeleteAsync<TModel>(TModel model, CancellationToken cancellationToken = default);

    public Task<ISmartTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    public ValueTask DisposeAsync();
}
```

`SmartContextOptions` carries:

- `IModelMetadataCache` (shared across contexts in DI; per-context metadata is computed once and cached).
- `string? SchemaOverride` — set per-request for multi-tenant.
- `IReadOnlyList<IGlobalQueryFilter>` — global filters that apply to every query (soft-delete, tenant, archived).
- `IReadOnlyDictionary<Type, ISmartValueConverter>` — converters for strongly-typed IDs / enums / value objects.
- `ISmartContextLogger? Logger` — query logging hook.
- `int CommandTimeoutSeconds`.

The static convenience API on `SmartModel<T>` is **preserved** but now operates against an *ambient* `SmartContext` resolved through DI/AsyncLocal:

```csharp
// Existing call site keeps working:
User user = await User.Query().Where(u => u.Email == "x").FirstAsync();

// New explicit form:
await using SmartContext context = serviceProvider.GetRequiredService<SmartContext>();
User user = await context.Query<User>().Where(u => u.Email == "x").FirstAsync(cancellationToken);
```

Static methods on `SmartModel<T>` delegate to `SmartContextAmbient.Current`, which is an `AsyncLocal<SmartContext>` populated by:

- ASP.NET Core middleware (`UseRadiantSmartContext()` extension), or
- Manual `using (SmartContextAmbient.Use(context))` scope.

If no ambient context is set, calls throw with a clear "no ambient SmartContext" message instead of the current vague "SmartModel has not been configured".

### 4.6 DI registration

```csharp
services.AddRadiant(builder =>
{
    builder.UsePostgreSql(connectionString, postgres =>
    {
        postgres.EnableDynamicJson();
        postgres.MaxPoolSize = 100;
    });
    builder.RegisterModelsFromAssembly(typeof(User).Assembly);
    builder.AddGlobalQueryFilter<ITenantScoped>(provider =>
    {
        ISessionAccessor session = provider.GetRequiredService<ISessionAccessor>();
        return (entity, query) => query.Where(nameof(ITenantScoped.WorkspaceId), "=", session.WorkspaceId);
    });
    builder.AddValueConverter<ContactId, string>(
        toDatabase: id => id.Value,
        fromDatabase: raw => new ContactId(raw));
});
```

`builder.AddValueConverter<TStrongId, TUnderlying>` is the entry point for `StronglyTypedIds`-style values. The `IPropertyMetadata` records both the CLR type and the resolved converter at registration time, so the visitors emit raw `string`/`Guid`/`long` parameters automatically.

### 4.7 Stronger LINQ expression visitor

Replace the existing `WhereExpressionVisitor` (binary-only) with a full `LinqToSqlTranslator` that produces an `IWhereExpression` tree, not a list of `(column, operator, value)` tuples:

Supported expression shapes:

| LINQ                                       | SQL                                               |
| ------------------------------------------ | ------------------------------------------------- |
| `x => x.Property == constant`              | `column = @p`                                     |
| `x => x.Property != constant`              | `column <> @p` (or `column IS NOT NULL` for null) |
| `x => x.Property == null`                  | `column IS NULL`                                  |
| `x => x.Property > value && other`         | `column > @p AND …`                               |
| `x => x.Property < value \|\| other`       | `column < @p OR …`                                |
| `x => !x.Property`                         | `NOT column`                                      |
| `x => x.Name.Contains("foo")`              | `column LIKE '%foo%'` (or `ILIKE` if configured)  |
| `x => x.Name.StartsWith("foo")`            | `column LIKE 'foo%'`                              |
| `x => x.Name.EndsWith("foo")`              | `column LIKE '%foo'`                              |
| `x => x.Name.ToLower() == "foo"`           | `LOWER(column) = @p`                              |
| `x => collection.Contains(x.Property)`     | `column IN (...)`                                 |
| `x => x.Property.HasValue`                 | `column IS NOT NULL`                              |
| `x => x.Foreign.Property == value`         | join with target table, filter target.column      |
| `x => x.Tags.Contains("admin")` (array)    | dialect array contains operator                   |
| `x => EF.Functions.JsonContains(…)` parity | adapter-specific JSON helpers                     |

Navigation property access (`x => x.Foreign.Property`) uses the same metadata cache to resolve the relationship and rewrites the query as a join (or sub-select, depending on adapter capability).

Method calls on `Radiant.Functions` (a static API mirroring `EF.Functions`) translate to dialect-specific SQL — for example `Radiant.Functions.JsonbContains(x.Emails, "[\"x@y.z\"]")` → `c.emails @> '["x@y.z"]'::jsonb` on Postgres.

### 4.8 Include / ThenInclude

```csharp
context.Query<Deal>()
       .Include(deal => deal.Activities)
       .Include(deal => deal.ContactRealEstateLink)
           .ThenInclude(link => link.Contact)
               .ThenInclude(contact => contact.Supervisor)
                   .ThenInclude(profile => profile.User)
       .Where(deal => deal.Id == dealId)
       .FirstOrDefaultAsync(cancellationToken);
```

Implementation:

- `Include` and `ThenInclude` return `ISmartIncludableQuery<TRoot, TCurrent>`. The current "include path" is tracked in metadata, not free-form strings.
- Loading strategy: **batched secondary queries**, not joins. For each include path, one query collects all foreign keys from the parent set and fetches the related rows in a single `WHERE foreign_key IN (...)`. Two-tier includes do an additional pass on the loaded children.
- Dot-notation `.Include("Activities.Comments")` still works and is normalised into the same path tree.
- Optional `IncludeFiltered<TChild>` (predicate on child): `Include(d => d.Activities, q => q.Where(a => !a.Archived))`.

This matches what Aether's `DealsService` needs and avoids the cartesian-explosion problem joins have when fanning out 4 collections.

### 4.9 Transactions

```csharp
await using ISmartTransaction transaction = await context.BeginTransactionAsync(cancellationToken: cancellationToken);

await context.Repository<Deal>().InsertAsync(deal, cancellationToken);
await context.Repository<DealActivity>().InsertAsync(activity, cancellationToken);

await transaction.CommitAsync(cancellationToken);
```

`SmartContext` holds an ambient `DbTransaction` while the transaction is open; all subsequent `Repository`/`Query`/`Raw` calls automatically enlist. Disposing without committing rolls back.

### 4.10 Bulk operations

```csharp
context.Repository<Deal>().InsertManyAsync(deals, cancellationToken);
context.Query<Deal>().Where(d => d.WorkspaceId == workspaceId)
                     .UpdateAsync(d => new { d.Archived = true }, cancellationToken);
context.Query<Deal>().Where(d => d.Archived).DeleteAsync(cancellationToken);
context.Repository<Deal>().UpsertAsync(deal, onConflict: d => d.MiniId, cancellationToken);
```

Bulk `InsertMany` uses adapter-specific batched-insert-with-returning when `BatchedInsertReturning` is supported; otherwise falls back to a single multi-row `INSERT … VALUES (…), (…), …`.

### 4.11 Global query filters

```csharp
builder.AddGlobalQueryFilter<ISoftDelete>(model => model.WhereNull(nameof(ISoftDelete.DeletedAt)));
builder.AddGlobalQueryFilter<ITenantScoped>((model, services) =>
{
    string workspaceId = services.GetRequiredService<ISessionAccessor>().WorkspaceId;
    return model.Where(nameof(ITenantScoped.WorkspaceId), "=", workspaceId);
});
```

Filters apply to every `Query<T>()` whose `T` implements the marker interface, including relationship loads. Opt-out per query via `IgnoreQueryFilter<TFilter>()`.

The existing `[SoftDelete]` attribute keeps working — it's just sugar over a `ISoftDelete` filter registered automatically.

### 4.12 Schema scoping (multi-tenant)

The current `_schemaOverride` field on `SmartQuery` becomes a context-level setting:

```csharp
SmartContext tenantContext = rootContext.WithSchema("workspace_abc123");
await tenantContext.Query<Contact>().ToListAsync(cancellationToken);
```

Implementation: `WithSchema` returns a lightweight context wrapper that overrides the resolved schema name for every table in every query. The metadata cache stays shared.

### 4.13 Audit columns / timestamps

`[Timestamps]` keeps working (writes `created_at`, `updated_at`). New built-in support:

- `[Audit]` attribute — `created_by_user_id`, `updated_by_user_id`, sourced from `ICurrentUserAccessor` resolved per context.
- Override `SmartContext.OnSaving(SmartModel model, SaveOperation operation)` for custom audit hooks.

### 4.14 Result mapping

Two interchangeable mappers via `IResultMapper`:

1. `DapperResultMapper` (default) — works today, no migration cost.
2. `SourceGeneratedResultMapper` — `Rymote.Radiant.Generators` emits per-model `Map(DbDataReader) → TModel` functions. Faster, AOT-friendly, no `dynamic`.

Strongly-typed value converters plug in at this layer too: `IResultMapper` consults `SmartContextOptions.ValueConverters` to turn `string raw` from the reader into `ContactId`.

### 4.15 Source generator scope

`SmartModelGenerator` keeps generating the typed `WhereX/WhereXContains/WhereXBetween` family. New outputs:

- `MapFrom(DbDataReader, ColumnLookup)` per model.
- `Project()` extension that emits column lists matching the generated mapper — eliminates `SELECT *`.
- `IncludeShortcut` properties on a `{ModelName}.Includes` static class, e.g. `User.Includes.Addresses` typed as `IncludePath<User, Address>` for compile-time-checked include paths.

`SqlExpressionsGenerator` keeps its current role (we'll verify exactly what it emits during implementation; if it's redundant, we remove it).

---

## 5. Migration path for existing callers

The public surface — `SmartModel.Configure()`, `User.Query()`, `await user.SaveAsync()`, `new SelectBuilder().From(…)` — remains source-compatible. Internally everything routes through the new adapter pipeline.

Steps for a caller already on Radiant (e.g. the Playground project and any external user):

1. Replace `services.AddSingleton<NpgsqlConnection>(…)` + `SmartModel.Configure(connection, cache)` with `services.AddRadiant(builder => builder.UsePostgreSql(connectionString).RegisterModelsFromAssembly(…))`.
2. Wire `app.UseRadiantSmartContext()` middleware (sets ambient `SmartContext` per request).
3. Optionally add `CancellationToken` to call sites (overloads without the token stay for compatibility).
4. No code changes for `User.Query().Where(…)` style.

Aether-specific adoption (separate effort, not covered by this spec): keep EF Core for migrations and complex tracked-entity scenarios; introduce Radiant `SmartContext` for new services and gradually migrate Dapper queries to `Query<T>()`.

---

## 6. Testing strategy

- **Adapter contract tests**: every `IDatabaseAdapter` implementation runs the same suite of "given builder X, compile to expected SQL Y" snapshot tests, parameterised by dialect.
- **In-memory adapter**: `Rymote.Radiant.Adapters.InMemory` (under `Rymote.Radiant.Testing`) executes against a `Dictionary<string, List<object>>` store; supports the subset of operations enough for unit tests of service code without spinning up a database.
- **Integration tests**: a `Testcontainers.PostgreSql` harness in `Rymote.Radiant.Adapters.PostgreSql.Tests` runs the full suite. The Playground is extended into a usable integration sample.
- **Round-trip**: every model in the Playground gets inserted, queried, updated, soft-deleted, restored, hard-deleted; assertions on the wire SQL via a SQL-capturing test adapter.

---

## 7. Risks and trade-offs

- **Scope is large.** ~116 .cs files to refactor. Mitigation: keep the SQL output byte-identical for Postgres, allowing existing integration tests to pin behaviour.
- **AsyncLocal ambient context** is convenient but can leak across awaits in unusual hosting models. We document the explicit `context.Query<T>()` form as the recommended path; the ambient form is for code that already uses the static `User.Query()` API.
- **Source-generated result mappers** require strict shape (constructor or settable properties). We keep Dapper as the default; opt in to generated mappers per model.
- **Capability flags surface adapter limits at runtime**, not compile time. Hard to do better without a separate type per dialect. We log a warning whenever a builder is constructed that requires a capability the active adapter lacks, even before `Build()` is called.

---

## 8. Acceptance criteria for v2.0

1. `Rymote.Radiant.csproj` references neither `Npgsql` nor any other DB driver.
2. `Rymote.Radiant.Adapters.PostgreSql` builds and runs the full existing Playground end-to-end against the same Postgres database; emitted SQL diff-equivalent to v1.
3. `SmartContext` is constructible via DI; ambient and explicit forms both work.
4. `LinqToSqlTranslator` handles every expression in the matrix in §4.7 (test-driven).
5. `Include`/`ThenInclude` produces O(depth) batched queries for the Aether `DealsService.GetDealByIdAsync` shape.
6. Transactions wrap multiple `Repository` calls atomically; rollback on dispose-without-commit.
7. Strongly-typed IDs round-trip through queries and inserts with a registered converter.
8. Global query filters apply to root queries and to includes; can be ignored per-query.
9. `CancellationToken` honoured by every async API; cancelling actually cancels the underlying `DbCommand`.
10. At least one stubbed second adapter (SqlServer or Sqlite) compiles a `SELECT 1` end-to-end, proving the abstraction holds.

---

## 9. Out of scope, deferred to v2.1

- Compiled query caching (memoise emitted SQL by tree shape).
- Change tracking / identity map.
- `IQueryable<T>` adapter on top of `ISmartQuery<T>` (some callers want LINQ-everywhere; possible but a separate effort).
- Migrations.
- Full SqlServer and MySql adapter functionality (skeletons only in v2.0).
- Distributed transactions / two-phase commit.

---

## 10. Glossary

- **Core**: `Rymote.Radiant` library — dialect-agnostic.
- **Adapter**: `Rymote.Radiant.Adapters.<Engine>` library — dialect-specific.
- **SmartContext**: instance replacement for the static `SmartModel.Configure()` API.
- **Ambient SmartContext**: an `AsyncLocal<SmartContext>` populated by middleware so `User.Query()` etc. still work statically.
- **Capability flags**: feature gates on `IDatabaseAdapter.Capabilities` (e.g., `JsonbColumn`).
- **Value converter**: function pair `(toDatabase, fromDatabase)` for strongly-typed IDs / enums / value objects.
- **Global query filter**: a predicate auto-applied to every `Query<T>` whose `T` matches a marker interface.
