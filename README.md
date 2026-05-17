<div align="center">
  Rymote.Radiant - Adapter-driven SQL builder and ORM for .NET 10
</div>

<div align="center">
  <sub>
    Brought to you by
    <a href="https://github.com/jovanivanovic">@jovanivanovic</a>,
    <a href="https://github.com/rymote">@rymote</a>
  </sub>
</div>

## Overview

Rymote.Radiant is a SQL builder and lightweight ORM for .NET 10. It is built around a strict adapter abstraction: the core library knows nothing about a specific database engine, and every dialect-specific concern — identifier quoting, parameter placeholder syntax, keyword vocabulary, JSON/array/vector/full-text operators, value casting — lives behind an `IDatabaseAdapter` implementation. PostgreSQL ships as the flagship adapter; a minimal SQLite adapter ships alongside it as a working proof of the abstraction.

The library has two layers stacked vertically. At the bottom is a fluent, type-safe SQL builder (`SelectBuilder`, `InsertBuilder`, `UpdateBuilder`, `DeleteBuilder`) backed by an `SqlEmitter` that walks clause and expression trees and emits dialect-correct SQL through the active adapter. On top sits a SmartModel layer with attribute-driven metadata, fluent `ISmartQuery<T>`, `ISmartRepository<T>`, `Include` / `ThenInclude`, transactions, global query filters, and an instance-based `SmartContext` registered through `Microsoft.Extensions.DependencyInjection`.

Both layers are usable independently. You can drop down to raw builders when you need precise control over the emitted SQL, or stay on the SmartModel surface for everyday CRUD and querying. The legacy `User.Query()` / `await user.SaveAsync()` static API from earlier versions still works without modification — it routes through an ambient `SmartContext` when one is present, and falls back to the original static configuration path when not.

## Key Features

- 🧩 **Adapter abstraction** — `IDatabaseAdapter` carries the dialect, identifier quoter, parameter formatter, value writer, and result mapper; every clause and expression emits through the adapter, never against hardcoded strings
- 🐘 **PostgreSQL-first** — full support for JSONB, arrays, vectors (pgvector), full-text search, ranges, CTEs, recursive CTEs, lateral joins, window functions, `RETURNING`, and `ON CONFLICT`
- 📦 **SQLite adapter included** — proof of the abstraction; `$pN` placeholders, `0`/`1` booleans, no `LATERAL`, no JSONB containment operators (the dialect throws `NotSupportedException` from properties the engine doesn't support)
- 🧱 **Fluent SQL builder** — typed clause graph that compiles to a `QueryCommand` carrying both the SQL text and ordered parameters; round-tripped through `IDatabaseAdapter.CreateCommand` for execution
- 🏷️ **Attribute-driven models** — `[Table]`, `[Column]`, `[PrimaryKey]`, `[ForeignKey]`, `[BelongsTo]`, `[HasOne]`, `[HasMany]`, `[Index]`, `[SoftDelete]`, `[Timestamps]`, `[Audit]`
- 🧠 **LINQ predicates** — `Where(x => x.IsActive && ids.Contains(x.Id) && x.Email != null)`; method calls on `string` (`Contains`, `StartsWith`, `EndsWith`), `Enumerable.Contains` (translated to `IN`), null comparisons, boolean property access, captured closures
- 🔗 **Include / ThenInclude** — typed chain via `IncludeChain(...).ThenInclude(...)`, plus dot-notation `Include("Parent.Child.Grandchild")` for deep navigation
- 💼 **Ambient transactions** — `BeginTransactionAsync` on `SmartContext`; every subsequent repository, query, and raw command enlists automatically
- 🏢 **Multi-tenant schema scoping** — `SmartContext.WithSchema("tenant_abc")` rewrites every emitted table reference to the chosen schema
- 🧵 **Async + CancellationToken** — every repository operation accepts a `CancellationToken` and threads it through to the underlying `DbCommand`
- 🧪 **Source generator** — `Rymote.Radiant.Generators` emits typed `WhereEmail`, `WhereEmailContains`, `WhereCreatedAtBetween`, `WhereTagsContains`, etc. for every model property
- 🔬 **Roslyn analyzers** — `Rymote.Radiant.Analyzers` catches common SmartModel and SQL builder mistakes at compile time
- 🧰 **DI integration** — `services.AddRadiant(builder => builder.UsePostgreSql(connectionString))` registers everything

## Projects

### [Rymote.Radiant](./Rymote.Radiant)
Core library. Defines the `IDatabaseAdapter` contract, the SQL builder graph, the `SqlEmitter`, the `QueryCompiler`, the SmartModel layer (`SmartModel`, `SmartContext`, `SmartQuery`, `SmartRepository`, `SmartRawQuery`), attribute-based metadata, the `LinqPredicateTranslator`, and the DI builder. Depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` and `System.Text.Json` — no database driver.

### [Rymote.Radiant.Adapters.PostgreSql](./Rymote.Radiant.Adapters.PostgreSql)
PostgreSQL adapter. Brings `Npgsql` and `Dapper` as dependencies. Implements `PostgreSqlDialect`, `PostgreSqlIdentifierQuoter`, `PostgreSqlParameterFormatter`, `PostgreSqlValueWriter`, `DapperResultMapper`, and the `UsePostgreSql(...)` builder extension. Full feature coverage: JSONB operators (`@>`, `<@`, `?`, `?|`, `?&`, `@?`, `@@`, `#-`, `||`), array operators, pgvector distance operators (`<->`, `<#>`, `<=>`, `<+>`), full-text search functions, range operators, RETURNING, ON CONFLICT, LATERAL joins.

### [Rymote.Radiant.Adapters.Sqlite](./Rymote.Radiant.Adapters.Sqlite)
SQLite adapter. Brings `Microsoft.Data.Sqlite` and `Dapper`. Demonstrates that the abstraction is real, not aspirational: SQLite uses `$pN` placeholders rather than PostgreSQL's `@pN`, emits `1`/`0` for booleans, and `NotSupportedException`s out of vector / range / lateral operators. An end-to-end test in the test project runs a compiled query against an in-memory SQLite database to prove the loop closes.

### [Rymote.Radiant.Analyzers](./Rymote.Radiant.Analyzers)
Roslyn analyzers. Catches misuse of the SmartModel attributes and the SQL builder at compile time.

### [Rymote.Radiant.Generators](./Rymote.Radiant.Generators)
Source generator. For every `SmartModel<T>` subclass, emits typed `Where{PropertyName}(...)`, `Where{PropertyName}Contains(...)`, `Where{PropertyName}Between(...)`, `Where{PropertyName}IsNull(...)`, `Where{PropertyName}After/Before/Today/ThisWeek/ThisMonth(...)`, and similar extension methods, scoped to the property's CLR type.

## Installation

Install the core package plus the adapter you need:

```bash
# Core (always required)
dotnet add package Rymote.Radiant

# PostgreSQL
dotnet add package Rymote.Radiant.Adapters.PostgreSql

# Or SQLite
dotnet add package Rymote.Radiant.Adapters.Sqlite
```

The core package transitively brings in `Rymote.Radiant.Analyzers` and `Rymote.Radiant.Generators`; no separate install required.

## Quick Start

### 1. Register Radiant in DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Adapters.PostgreSql.DependencyInjection;
using Rymote.Radiant.Smart.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRadiant(radiantBuilder =>
{
    radiantBuilder.UsePostgreSql(
        builder.Configuration.GetConnectionString("Default")!,
        dataSourceBuilder =>
        {
            dataSourceBuilder.EnableDynamicJson();
        });

    radiantBuilder.RegisterModelsFromAssembly(typeof(User).Assembly);
});

var app = builder.Build();
```

### 2. Wire an ambient `SmartContext` per request

For ASP.NET Core, set the ambient context once per HTTP request so the legacy static `User.Query()` API also works:

```csharp
using Rymote.Radiant.Smart.Context;

app.Use(async (httpContext, next) =>
{
    SmartContext context = httpContext.RequestServices.GetRequiredService<SmartContext>();
    using (SmartContextAmbient.Use(context))
        await next(httpContext);
});
```

### 3. Define a model

```csharp
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

[Table("users")]
[Timestamps]
[SoftDelete]
public class User : SmartModel<User>
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("profile_data", databaseType: "jsonb")]
    public string ProfileData { get; set; } = "{}";

    [Column("tags", databaseType: "text[]")]
    public string[]? Tags { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    [HasMany(typeof(Order), "UserId")]
    public List<Order> Orders { get; set; } = new();
}
```

### 4. Query and mutate

```csharp
// Using the instance-based SmartContext (recommended)
await using SmartContext context = serviceProvider.GetRequiredService<SmartContext>();

User? user = await context.Query<User>()
    .Where(candidate => candidate.IsActive && candidate.Email == "alice@example.com")
    .FirstOrDefaultAsync();

await context.Repository<User>().InsertAsync(new User
{
    Email = "bob@example.com",
    Username = "bob"
});

// Or the legacy static API (works when an ambient context is set)
User? sameUser = await User.Query()
    .Where(candidate => candidate.Email == "alice@example.com")
    .FirstOrDefaultAsync();

await sameUser!.SaveAsync();
```

## Defining Models

Models inherit from `SmartModel<TModel>` and use attributes to describe the schema mapping.

### Attribute reference

| Attribute | Purpose |
|---|---|
| `[Table(name, schema = null)]` | Maps the class to a table. Optional schema name. |
| `[Column(name, databaseType = null)]` | Maps a property to a column. `databaseType` overrides the inferred type (e.g. `"jsonb"`, `"text[]"`, `"vector"`, `"tsvector"`). |
| `[PrimaryKey]` | Marks the primary key. Combine with `IsAutoIncrement` on the metadata for serial/identity columns. |
| `[ForeignKey(referencedTable, referencedColumn)]` | Declares a foreign key. Used by metadata, not for runtime cascade. |
| `[BelongsTo(typeof(Parent), foreignKeyProperty)]` | Many-to-one navigation. Loaded via `Include`. |
| `[HasOne(typeof(Child), foreignKeyProperty)]` | One-to-one navigation. |
| `[HasMany(typeof(Child), foreignKeyProperty)]` | One-to-many navigation. |
| `[Index(columns, name = null, isUnique = false)]` | Declares an index. Pure metadata; not auto-applied. |
| `[SoftDelete]` | Adds an implicit `WHERE deleted_at IS NULL` to every query. Toggle off with `.WithTrashed()` or `.OnlyTrashed()`. |
| `[Timestamps]` | Auto-populates `CreatedAt` / `UpdatedAt` on insert and update. |
| `[Audit]` | Hooks for `CreatedByUserId` / `UpdatedByUserId` via an `ICurrentUserAccessor` (wiring is v3.1). |

### Strongly-typed IDs

Register a value converter so an `OrderId` value object round-trips cleanly:

```csharp
radiantBuilder.AddValueConverter<OrderId, string>(
    toDatabase: orderId => orderId.Value,
    fromDatabase: rawValue => new OrderId(rawValue));
```

(Full converter wiring at insert/update and result-mapping time is on the v3.1 roadmap; the registration surface is stable.)

## Querying

### LINQ predicates

`SmartQuery<T>.Where(predicate)` translates expression trees through `LinqPredicateTranslator`. Supported shapes:

| LINQ | SQL |
|---|---|
| `user => user.Id == 5` | `"id" = @p0` |
| `user => user.Email != null` | `"email" IS NOT NULL` |
| `user => user.Age >= 18 && user.IsActive` | `"age" >= @p0 AND "is_active" = @p1` |
| `user => user.Username.StartsWith("admin")` | `"username" LIKE 'admin%'` |
| `user => user.Email.Contains("@example.com")` | `"email" LIKE '%@example.com%'` |
| `user => ids.Contains(user.Id)` | `"id" IN (@p0, @p1, ...)` |
| `user => user.IsActive` | `"is_active" = TRUE` |
| `user => !user.IsArchived` | `"is_archived" = FALSE` |

The translator also accepts the source-generator typed helpers:

```csharp
context.Query<User>()
    .WhereEmailContains("@example.com")
    .WhereCreatedAtAfter(DateTime.UtcNow.AddDays(-7))
    .WhereTagsContains("admin")
    .ToListAsync();
```

### Aggregates and projections

```csharp
int activeCount = await context.Query<User>().Where(user => user.IsActive).CountAsync();
bool any        = await context.Query<User>().Where(user => user.Email == "x").AnyAsync();
decimal total   = await context.Query<Order>().SumAsync(order => order.AmountNet);
double average  = await context.Query<Order>().AverageAsync(order => order.AmountNet);
DateTime? max   = await context.Query<User>().MaxAsync(user => user.CreatedAt);
```

### Include / ThenInclude

Dot-notation is supported on the base `Include(string)`:

```csharp
await context.Query<Order>()
    .Include("OrderShipment.Customer.AccountManager.User")
    .Where(order => order.Id == orderId)
    .FirstOrDefaultAsync();
```

Or use the typed chain:

```csharp
using Rymote.Radiant.Smart.Loading;

await context.Query<Order>()
    .IncludeChain(order => order.OrderShipment)
        .ThenInclude(shipment => shipment.Customer)
        .ThenInclude(customer => customer.AccountManager)
        .ThenInclude(accountManager => accountManager.User)
    .Where(order => order.Id == orderId)
    .FirstOrDefaultAsync();
```

Both forms feed the same batched relationship loader: one secondary `SELECT ... WHERE foreign_key IN (...)` per level, regardless of result set size.

### Soft delete

Models annotated with `[SoftDelete]` are filtered out by default. Override per-query:

```csharp
List<User> deletedUsers   = await context.Query<User>().OnlyTrashed().ToListAsync();
List<User> includingTrash = await context.Query<User>().WithTrashed().ToListAsync();
```

### Raw SQL escape hatch

```csharp
List<DashboardRow> rows = await context.Raw().QueryAsync<DashboardRow>(
    sql:        "SELECT date_trunc('day', created_at) AS day, COUNT(*) AS users " +
                "FROM users WHERE created_at >= @startDate GROUP BY 1 ORDER BY 1",
    parameters: new { startDate = DateTime.UtcNow.AddDays(-30) });
```

`Raw()` enlists on the ambient transaction automatically and accepts a `CancellationToken`.

## Mutations

### Insert, Update, Save

```csharp
User user = new User { Email = "alice@example.com", Username = "alice" };
await context.Repository<User>().InsertAsync(user);
// user.Id is now populated (RETURNING id)

user.Username = "alice.new";
await context.Repository<User>().UpdateAsync(user);

// Or via SaveAsync — chooses INSERT or UPDATE by primary-key value
await user.SaveAsync();
```

### Upsert

```csharp
await context.Repository<User>().UpsertAsync(user);  // Insert when PK is default, Update otherwise
```

(Native `INSERT … ON CONFLICT DO UPDATE` one-trip upsert is on the v3.1 roadmap; `OnConflictClause` is already wired into `InsertBuilder` for raw-builder users.)

### Bulk insert

```csharp
List<User> newUsers = LoadUsersFromCsv();
IReadOnlyList<User> inserted = await context.Repository<User>().InsertManyAsync(newUsers);
```

### Delete and soft delete

```csharp
bool deleted = await context.Repository<User>().DeleteAsync(user);

// Soft delete (when [SoftDelete] is present)
bool softDeleted = await context.Repository<User>().SoftDeleteAsync(user);
bool restored    = await context.Repository<User>().RestoreAsync(user);

// Bypass soft delete and hard-delete
bool hardDeleted = await context.Repository<User>().ForceDeleteAsync(user);
```

## Transactions

```csharp
await using SmartContext context = serviceProvider.GetRequiredService<SmartContext>();
await using ISmartTransaction transaction = await context.BeginTransactionAsync(cancellationToken: cancellationToken);

await context.Repository<Order>().InsertAsync(order, cancellationToken);
await context.Repository<OrderItem>().InsertManyAsync(items, cancellationToken);
await context.Raw().ExecuteAsync(
    "UPDATE tenant_stats SET order_count = order_count + 1 WHERE tenant_id = @id",
    new { id = tenantId });

await transaction.CommitAsync(cancellationToken);
```

The transaction stays on the `SmartContext` for its lifetime. Every subsequent `Repository`, `Query`, and `Raw` call enlists automatically. Disposing without committing rolls back.

## Multi-tenant schema scoping

```csharp
SmartContext tenantContext = rootContext.WithSchema("tenant_abc123");
List<Customer> customers = await tenantContext.Query<Customer>().ToListAsync();
// Emitted SQL: SELECT ... FROM "tenant_abc123"."customers" ...
```

The schema name flows into every emitted `FromClause`, including those produced by `Include` / `ThenInclude` relationship loads. The metadata cache stays shared across tenant contexts.

## Adapters and capabilities

Each adapter declares which features it supports via `DatabaseCapabilities`:

```csharp
[Flags]
public enum DatabaseCapabilities : long
{
    ReturningClause, UpsertOnConflict, UpsertMerge,
    CommonTableExpression, RecursiveCommonTableExpression,
    LateralJoin, WindowFunctions, SchemaPerTable,
    JsonbColumn, ArrayColumn, VectorColumn, FullTextSearch, RangeTypes,
    CaseInsensitiveText, SpatialTypes, CaseInsensitiveLikeOperator,
    RegularExpressionOperator, NamedSequences, BatchedInsertReturning,
}
```

Check capabilities before using engine-specific features:

```csharp
if (context.Adapter.Capabilities.HasFlag(DatabaseCapabilities.JsonbColumn))
{
    query = query.WhereJsonbContains(user => user.ProfileData, new { plan = "premium" });
}
```

| Capability | PostgreSQL | SQLite |
|---|---|---|
| `ReturningClause` | ✅ | ✅ (3.35+) |
| `UpsertOnConflict` | ✅ | ✅ |
| `CommonTableExpression` | ✅ | ✅ |
| `RecursiveCommonTableExpression` | ✅ | ✅ |
| `LateralJoin` | ✅ | ❌ |
| `WindowFunctions` | ✅ | ✅ |
| `SchemaPerTable` | ✅ | ❌ |
| `JsonbColumn` | ✅ | ❌ |
| `ArrayColumn` | ✅ | ❌ |
| `VectorColumn` | ✅ (pgvector) | ❌ |
| `FullTextSearch` | ✅ | partial (FTS5) |
| `RangeTypes` | ✅ | ❌ |
| `CaseInsensitiveText` | ✅ (citext) | ❌ |
| `CaseInsensitiveLikeOperator` | ✅ (ILIKE) | ❌ (LIKE is CI by default) |
| `RegularExpressionOperator` | ✅ | ❌ |
| `BatchedInsertReturning` | ✅ | ❌ |

## PostgreSQL-specific features

### JSONB

```csharp
context.Query<User>()
    .WhereJsonbContains(user => user.ProfileData, new { plan = "premium" })
    .WhereJsonbHasKey(user => user.ProfileData, "verified")
    .WhereJsonbHasAnyKeys(user => user.ProfileData, "email_verified", "phone_verified")
    .WhereJsonbPathExists(user => user.ProfileData, "$.preferences.language")
    .ToListAsync();
```

### Arrays

```csharp
context.Query<User>()
    .WhereArrayContains<string>(user => user.Tags, "admin", "moderator")
    .WhereArrayOverlaps<string>(user => user.Tags, "vip", "premium")
    .ToListAsync();
```

### Full-text search

```csharp
context.Query<Article>()
    .WhereFullTextSearch(article => article.Body, "linq adapter pattern", language: "english")
    .OrderByFullTextRank(article => article.Body, "linq adapter pattern")
    .ToListAsync();
```

### Vector similarity (pgvector)

```csharp
using Rymote.Radiant.Sql.Expressions;

float[] queryEmbedding = await embeddingService.EmbedAsync("similar to this");

context.Query<Document>()
    .WhereVectorSimilarity(
        document => document.Embedding,
        queryEmbedding,
        VectorOperator.CosineDistance,
        threshold: 0.3f)
    .OrderByVectorDistance(
        document => document.Embedding,
        queryEmbedding,
        VectorOperator.CosineDistance)
    .Take(10)
    .ToListAsync();
```

### CTEs and recursive CTEs

```csharp
SelectBuilder rootCategories = new SelectBuilder()
    .Select(new ColumnExpression("id"), new ColumnExpression("name"), new ColumnExpression("parent_id"), new RawSqlExpression("0 AS level"))
    .From("categories")
    .WhereNull("parent_id");

context.Query<Category>()
    .WithRecursive("category_tree", rootCategories)
    .Where("parent_id", "IS NOT", null)
    .ToListAsync();
```

## SQL builder (low level)

When you need precise control, drop down to the builder:

```csharp
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;

SelectBuilder selectBuilder = new SelectBuilder()
    .Select(
        new ColumnExpression("id"),
        new ColumnExpression("email"),
        new FunctionExpression("COUNT", new ColumnExpression("order_id")).As("order_count"))
    .From("users", schemaName: "public", alias: "u")
    .LeftJoin("orders", leftColumn: "u.id", rightColumn: "orders.user_id", alias: "orders")
    .Where("u.is_active", "=", true)
    .GroupBy("u.id", "u.email")
    .Having("COUNT(orders.id)", ">=", 5)
    .OrderBy("order_count", SortDirection.Descending)
    .Limit(25, offset: 0);

QueryCommand compiled = selectBuilder.Build(context.Adapter);
IEnumerable<UserOrderSummary> rows = await context.Adapter.ResultMapper
    .QueryAsync<UserOrderSummary>(/* DbCommand */, cancellationToken);
```

`Build(adapter)` returns a `QueryCommand` with the dialect-correct SQL text and ordered parameter list. The legacy `Build()` (no adapter) remains for backward compatibility and emits PostgreSQL-flavoured SQL.

## Writing a new adapter

Adding a new database engine is mechanical — implement six interfaces and register a builder extension:

```csharp
public sealed class MySqlDialect          : ISqlDialect          { /* keywords, sub-dialects */ }
public sealed class MySqlIdentifierQuoter : IIdentifierQuoter    { /* `name` backtick quoting  */ }
public sealed class MySqlParameterFormatter : IParameterFormatter { /* "?" placeholders        */ }
public sealed class MySqlValueWriter      : IValueWriter         { /* literal serialization     */ }
public sealed class MySqlResultMapper     : IResultMapper        { /* DbCommand -> TResult     */ }

public sealed class MySqlAdapter : IDatabaseAdapter
{
    public string Identifier => "mysql";
    public DatabaseCapabilities Capabilities => /* ... */;
    public ISqlDialect Dialect { get; } = new MySqlDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new MySqlIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new MySqlParameterFormatter();
    public IValueWriter ValueWriter { get; } = new MySqlValueWriter();
    public IResultMapper ResultMapper { get; } = new MySqlResultMapper();

    public DbConnection CreateConnection() { /* MySqlConnection */ }
    public Task<DbConnection> OpenConnectionAsync(CancellationToken token) { /* ... */ }
    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiled) { /* ... */ }
}

public static class MySqlBuilderExtensions
{
    public static RadiantBuilder UseMySql(this RadiantBuilder builder, string connectionString)
        => builder.UseAdapter(_ => new MySqlAdapter(connectionString));
}
```

Zero changes to the core library or any clause/expression. The SQLite adapter (`Rymote.Radiant.Adapters.Sqlite`) is itself a worked example of this pattern.

## Configuration reference

```csharp
services.AddRadiant(builder =>
{
    builder.UsePostgreSql(connectionString, dataSourceBuilder =>
    {
        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
    });

    builder.RegisterModelsFromAssembly(typeof(User).Assembly);

    builder.AddValueConverter<OrderId, string>(
        toDatabase: id => id.Value,
        fromDatabase: raw => new OrderId(raw));

    builder.AddGlobalQueryFilter(new TenantQueryFilter());

    builder.WithCommandTimeout(60);
});
```

`SmartContext` is registered as `Scoped` — one per DI scope (per request in ASP.NET Core). The connection is opened lazily on first use; `Dispose` / `DisposeAsync` releases it.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Code                         │
│  context.Query<User>().Where(u => u.IsActive).ToListAsync()     │
└─────────────────────────┬───────────────────────────────────────┘
                          │
              ┌───────────▼───────────┐
              │      SmartContext     │  scoped DI; connection + tx + schema + filters
              └───────────┬───────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  SmartQuery  │  │ SmartRepo... │  │SmartRawQuery │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       └─────────────────┼─────────────────┘
                         │
                ┌────────▼────────┐
                │   SQL Builder   │  SelectBuilder / InsertBuilder / etc.
                └────────┬────────┘
                         │
                ┌────────▼────────┐
                │ QueryCompiler   │  walks clauses via Accept(SqlEmitter)
                └────────┬────────┘
                         │
                ┌────────▼────────┐
                │   SqlEmitter    │  Buffer + ParameterBag + active Adapter
                └────────┬────────┘
                         │
        ┌────────────────┼────────────────┐
        ▼                ▼                ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ ISqlDialect  │  │IIdentifierQ. │  │IParameterFmt │  ... + IValueWriter + IResultMapper
└──────────────┘  └──────────────┘  └──────────────┘
                         │
                ┌────────▼────────┐
                │IDatabaseAdapter │  PostgreSqlAdapter / SqliteAdapter / ...
                └────────┬────────┘
                         │
                ┌────────▼────────┐
                │   DbConnection  │
                └─────────────────┘
```

## Testing

`Rymote.Radiant.Tests` (xUnit) covers:

- **LinqPredicateTranslator** — every supported predicate shape
- **SmartContextAmbient** — DI registration, ambient-context Use/Dispose semantics
- **AdapterCompilePath** — cross-path snapshot tests proving the new adapter-aware compile produces byte-identical SQL to the legacy AppendTo path for SELECT / INSERT-RETURNING / UPDATE / DELETE / DISTINCT / JSONB
- **SqliteAdapter** — placeholder prefix differs from PostgreSQL, capabilities flags correctly exclude unsupported features, end-to-end execution against an in-memory SQLite database

Run with:

```bash
dotnet test Rymote.Radiant.Tests/Rymote.Radiant.Tests.csproj
```

## Roadmap (v3.1 and beyond)

- Value-converter wiring at insert/update and result-mapping time (registration surface is stable; conversion isn't yet applied automatically by `SmartRepository`).
- Native `INSERT … ON CONFLICT DO UPDATE` upsert.
- `Query.UpdateAsync(setterExpression)` and `Query.DeleteAsync()` bulk operations driven by a `LinqToSetTranslator`.
- `CancellationToken` overloads across the entire `ISmartQuery<T>` surface (currently the repository surface is fully tokenised; the query surface still uses the legacy parameterless `*Async` methods).
- Source-generated result mappers as an opt-in alternative to Dapper's reflection-based mapping.
- Removal of the legacy `AppendTo` path once external callers migrate to `Build(adapter)`.
- SqlServer and MySQL adapter projects.

## License

This project is licensed under the MIT License — see [LICENSE.md](./LICENSE.md) for details.

## Authors

- [@jovanivanovic](https://github.com/jovanivanovic)
- [@rymote](https://github.com/rymote)
