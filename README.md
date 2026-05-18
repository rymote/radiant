<div align="center">
    <a href="https://github.com/rymote/radiant"><img src="https://github.com/rymote/radiant/blob/master/.github/rymote-radiant-cover.png" alt="rymote/radiant" /></a>
</div>
<br />

<div align="center">
  Rymote.Radiant - Adapter-driven SQL builder and ORM for .NET
</div>

<div align="center">
  <sub>
    Brought to you by
    <a href="https://github.com/jovanivanovic">@jovanivanovic</a>,
    <a href="https://github.com/rymote">@rymote</a>
  </sub>
</div>

## Overview

Rymote.Radiant is a SQL builder and lightweight ORM for .NET 10. It is built around a strict adapter abstraction: the core library knows nothing about a specific database engine, and every dialect-specific concern — identifier quoting, parameter placeholder syntax, keyword vocabulary, JSON/array/vector/full-text operators, value casting — lives behind an `IDatabaseAdapter` implementation. Four adapters ship in the box: PostgreSQL (full feature coverage including pgvector and full-text), SQLite (in-memory and file-based), Microsoft SQL Server, and MySQL.

The library has two layers stacked vertically. At the bottom is a fluent, type-safe SQL builder (`SelectBuilder`, `InsertBuilder`, `UpdateBuilder`, `DeleteBuilder`) backed by an `SqlEmitter` that walks clause and expression trees and emits dialect-correct SQL through the active adapter. On top sits a SmartModel layer with attribute-driven metadata, fluent `ISmartQuery<T>`, `ISmartRepository<T>`, `Include` / `ThenInclude`, transactions, global query filters, and an instance-based `SmartContext` registered through `Microsoft.Extensions.DependencyInjection`.

Both layers are usable independently. You can drop down to raw builders when you need precise control over the emitted SQL, or stay on the SmartModel surface for everyday CRUD and querying. The legacy `User.Query()` / `await user.SaveAsync()` static API from earlier versions still works without modification — it routes through an ambient `SmartContext` when one is present, and falls back to the original static configuration path when not.

## Features

- **Adapter abstraction** — `IDatabaseAdapter` carries the dialect, identifier quoter, parameter formatter, value writer, and result mapper; every clause and expression emits through the adapter, never against hardcoded strings.
- **PostgreSQL adapter** — full support for JSONB, arrays, vectors (pgvector), full-text search, ranges, CTEs, recursive CTEs, lateral joins, window functions, `RETURNING`, and `ON CONFLICT`.
- **SQLite adapter** — `$pN` placeholders, `0`/`1` booleans, FTS5 full-text, `RETURNING` (3.35+), `ON CONFLICT` upsert.
- **SQL Server adapter** — `[bracket]` identifier quoting, `@pN` parameters, `OUTPUT` clause for returning rows, T-SQL keyword vocabulary.
- **MySQL adapter** — backtick identifier quoting, `@pN` parameters, JSON `->`/`->>` extract operators, full-text `MATCH AGAINST`.
- **Fluent SQL builder** — typed clause graph that compiles to a `QueryCommand` carrying both the SQL text and ordered parameters; round-tripped through `IDatabaseAdapter.CreateCommand` for execution.
- **Attribute-driven models** — `[Table]`, `[Column]`, `[PrimaryKey]`, `[ForeignKey]`, `[BelongsTo]`, `[HasOne]`, `[HasMany]`, `[Index]`, `[SoftDelete]`, `[Timestamps]`, `[Audit]`.
- **LINQ predicates** — `Where(x => x.IsActive && ids.Contains(x.Id) && x.Email != null)`; method calls on `string` (`Contains`, `StartsWith`, `EndsWith`), `Enumerable.Contains` (translated to `IN`), null comparisons, boolean property access, captured closures.
- **Bulk operations** — `Query<T>().Where(...).UpdateAsync(x => new { x.Archived = true })` and `Query<T>().Where(...).DeleteAsync()` emit single SQL statements with no client-side iteration.
- **Include / ThenInclude** — typed chain via `IncludeChain(...).ThenInclude(...)`, plus dot-notation `Include("Parent.Child.Grandchild")` for deep navigation.
- **Ambient transactions** — `BeginTransactionAsync` on `SmartContext`; every subsequent repository, query, and raw command enlists automatically.
- **Multi-tenant schema scoping** — `SmartContext.WithSchema("tenant_abc")` rewrites every emitted table reference to the chosen schema.
- **Strongly-typed IDs** — register a `ValueConverter<TClr, TDatabase>` and Radiant applies it at every boundary.
- **Async + CancellationToken** — every async method on `ISmartRepository<T>` and `ISmartQuery<T>` accepts a `CancellationToken` and threads it through to the underlying `DbCommand`.
- **Source-generated result mappers** — zero-allocation hot path, AOT-friendly.
- **Roslyn analyzers** — catch common SmartModel and SQL builder mistakes at compile time.
- **DI integration** — `services.AddRadiant(builder => builder.UsePostgreSql(connectionString))` registers everything.

## Projects

### [Rymote.Radiant](./Rymote.Radiant)
Core library. Defines the `IDatabaseAdapter` contract, the SQL builder graph, the `SqlEmitter`, the `QueryCompiler`, the SmartModel layer (`SmartModel`, `SmartContext`, `SmartQuery`, `SmartRepository`, `SmartRawQuery`), attribute-based metadata, the `LinqPredicateTranslator`, and the DI builder. Depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` and `System.Text.Json` — no database driver.

### [Rymote.Radiant.Adapters.PostgreSql](./Rymote.Radiant.Adapters.PostgreSql)
PostgreSQL adapter. Brings `Npgsql` and `Dapper` as dependencies. Full feature coverage: JSONB operators, array operators, pgvector distance operators, full-text search functions, range operators, RETURNING, ON CONFLICT, LATERAL joins.

### [Rymote.Radiant.Adapters.Sqlite](./Rymote.Radiant.Adapters.Sqlite)
SQLite adapter. Brings `Microsoft.Data.Sqlite` and `Dapper`. Uses `$pN` placeholders, emits `1`/`0` for booleans, supports FTS5 full-text, `RETURNING` (3.35+), and `ON CONFLICT` upsert.

### [Rymote.Radiant.Adapters.SqlServer](./Rymote.Radiant.Adapters.SqlServer)
Microsoft SQL Server adapter. Brings `Microsoft.Data.SqlClient` and `Dapper`. Uses `[bracket]` identifier quoting, `@pN` named parameters, the T-SQL `OUTPUT` clause for `RETURNING`-style row hydration, and `1`/`0` bit literals for booleans.

### [Rymote.Radiant.Adapters.MySql](./Rymote.Radiant.Adapters.MySql)
MySQL adapter. Brings `MySqlConnector` and `Dapper`. Uses backtick identifier quoting, `@pN` named parameters, `1`/`0` bit literals, and `MATCH AGAINST` full-text. JSON access via `->` and `->>` is supported.

### [Rymote.Radiant.Analyzers](./Rymote.Radiant.Analyzers)
Roslyn analyzers. Catches misuse of the SmartModel attributes and the SQL builder at compile time.

### [Rymote.Radiant.Generators](./Rymote.Radiant.Generators)
Source generator. Emits typed `Where{PropertyName}(...)` helpers and per-model `DbDataReader → CLR` result mappers that auto-register via `[ModuleInitializer]`.

## Installation

Install the core package plus the adapter you need:

```bash
# Core (always required)
dotnet add package Rymote.Radiant

# Pick one or more adapters:
dotnet add package Rymote.Radiant.Adapters.PostgreSql
dotnet add package Rymote.Radiant.Adapters.Sqlite
dotnet add package Rymote.Radiant.Adapters.SqlServer
dotnet add package Rymote.Radiant.Adapters.MySql
```

The core package transitively brings in `Rymote.Radiant.Analyzers` and `Rymote.Radiant.Generators`; no separate install required.

## Quick start

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

## Defining models

Models inherit from `SmartModel<TModel>` and use attributes to describe the schema mapping.

### Attribute reference

| Attribute | Purpose |
|---|---|
| `[Table(name, schema = null)]` | Maps the class to a table. Optional schema name. |
| `[Column(name, databaseType = null)]` | Maps a property to a column. `databaseType` overrides the inferred type. |
| `[PrimaryKey]` | Marks the primary key. |
| `[ForeignKey(referencedTable, referencedColumn)]` | Declares a foreign key. |
| `[BelongsTo(typeof(Parent), foreignKeyProperty)]` | Many-to-one navigation. Loaded via `Include`. |
| `[HasOne(typeof(Child), foreignKeyProperty)]` | One-to-one navigation. |
| `[HasMany(typeof(Child), foreignKeyProperty)]` | One-to-many navigation. |
| `[Index(columns, name = null, isUnique = false)]` | Declares an index. |
| `[SoftDelete]` | Adds an implicit `WHERE deleted_at IS NULL` to every query. |
| `[Timestamps]` | Auto-populates `CreatedAt` / `UpdatedAt` on insert and update. |
| `[Audit]` | Hooks for `CreatedByUserId` / `UpdatedByUserId` via an `ICurrentUserAccessor`. |

### Strongly-typed IDs

```csharp
radiantBuilder.AddValueConverter<OrderId, string>(
    toDatabase: orderId => orderId.Value,
    fromDatabase: rawValue => new OrderId(rawValue));
```

The converter is applied automatically at every boundary: during `InsertAsync` / `UpdateAsync`, when capturing values inside `Where`, when `SmartModel.FindAsync(...)` builds its lookup, and when Dapper materializes a result row.

## Querying

### LINQ predicates

| LINQ | SQL |
|---|---|
| `user => user.Id == 5` | `"id" = @p0` |
| `user => user.Email != null` | `"email" IS NOT NULL` |
| `user => user.Age >= 18 && user.IsActive` | `"age" >= @p0 AND "is_active" = @p1` |
| `user => user.Username.StartsWith("admin")` | `"username" LIKE 'admin%'` |
| `user => user.Email.Contains("@example.com")` | `"email" LIKE '%@example.com%'` |
| `user => ids.Contains(user.Id)` | `"id" IN (@p0, @p1, ...)` |
| `user => user.IsActive` | `"is_active" = TRUE` |

### Typed query extensions

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

```csharp
await context.Query<Order>()
    .Include("OrderShipment.Customer.AccountManager.User")
    .Where(order => order.Id == orderId)
    .FirstOrDefaultAsync();
```

Or the typed chain:

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

### Raw SQL escape hatch

```csharp
List<DashboardRow> rows = await context.Raw().QueryAsync<DashboardRow>(
    sql:        "SELECT date_trunc('day', created_at) AS day, COUNT(*) AS users " +
                "FROM users WHERE created_at >= @startDate GROUP BY 1 ORDER BY 1",
    parameters: new { startDate = DateTime.UtcNow.AddDays(-30) });
```

## Mutations

```csharp
User user = new User { Email = "alice@example.com", Username = "alice" };
await context.Repository<User>().InsertAsync(user);

user.Username = "alice.new";
await context.Repository<User>().UpdateAsync(user);

await user.SaveAsync();

// Upsert
User hydrated = await context.Repository<User>().UpsertAsync(user);

// Bulk insert
IReadOnlyList<User> inserted = await context.Repository<User>().InsertManyAsync(newUsers);

// Soft delete
bool softDeleted = await context.Repository<User>().SoftDeleteAsync(user);
bool restored    = await context.Repository<User>().RestoreAsync(user);
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

The transaction stays on the `SmartContext` for its lifetime. Every subsequent `Repository`, `Query`, and `Raw` call enlists automatically.

## Multi-tenant schema scoping

```csharp
SmartContext tenantContext = rootContext.WithSchema("tenant_abc123");
List<Customer> customers = await tenantContext.Query<Customer>().ToListAsync();
// Emitted SQL: SELECT ... FROM "tenant_abc123"."customers" ...
```

## Adapters and capabilities

| Capability | PostgreSQL | SQLite | SQL Server | MySQL |
|---|---|---|---|---|
| `ReturningClause` | ✅ | ✅ (3.35+) | ✅ (`OUTPUT`) | ❌ |
| `UpsertOnConflict` | ✅ | ✅ | ❌ (uses `MERGE`) | ❌ (uses `ON DUPLICATE KEY UPDATE`) |
| `CommonTableExpression` | ✅ | ✅ | ✅ | ✅ |
| `RecursiveCommonTableExpression` | ✅ | ✅ | ✅ | ✅ |
| `LateralJoin` | ✅ | ❌ | ✅ (`CROSS APPLY`) | ❌ |
| `WindowFunctions` | ✅ | ✅ | ✅ | ✅ |
| `SchemaPerTable` | ✅ | ❌ | ✅ | ✅ |
| `JsonbColumn` | ✅ | ❌ | ❌ | partial (`->`, `->>` only) |
| `ArrayColumn` | ✅ | ❌ | ❌ | ❌ |
| `VectorColumn` | ✅ (pgvector) | ❌ | ❌ | ❌ |
| `FullTextSearch` | ✅ | partial (FTS5) | partial (`CONTAINS`/`FREETEXT`) | ✅ (`MATCH AGAINST`) |
| `RangeTypes` | ✅ | ❌ | ❌ | ❌ |
| `CaseInsensitiveText` | ✅ (citext) | ❌ | ✅ (collation) | ✅ (collation) |
| `CaseInsensitiveLikeOperator` | ✅ (`ILIKE`) | ❌ | ❌ | ❌ |
| `RegularExpressionOperator` | ✅ | ❌ | ❌ | ✅ (`REGEXP`) |
| `BatchedInsertReturning` | ✅ | ❌ | ✅ | ❌ |

## SQL builder (low level)

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
```

`Build(adapter)` returns a `QueryCommand` with the dialect-correct SQL text and ordered parameter list.

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
                ┌────────▼────────┐
                │IDatabaseAdapter │  PostgreSqlAdapter / SqliteAdapter / ...
                └────────┬────────┘
                         │
                ┌────────▼────────┐
                │   DbConnection  │
                └─────────────────┘
```

## Testing

```bash
dotnet test Rymote.Radiant.Tests/Rymote.Radiant.Tests.csproj
```

## Support the project

If Radiant has helped you ship faster, please consider supporting ongoing development:

- [Patreon](https://www.patreon.com/rymote)
- [Open Collective](https://opencollective.com/rymote)

## License

This project is licensed under the BSD 3-Clause License — see [LICENSE.md](./LICENSE.md) for details.

## Authors

- [@jovanivanovic](https://github.com/jovanivanovic)
- [@rymote](https://github.com/rymote)
