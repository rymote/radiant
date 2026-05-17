# Rymote.Radiant — Project Notes

A two-layer database access library for .NET:

1. **`Rymote.Radiant`** (core, dialect-agnostic): SQL builders (`SelectBuilder`, `InsertBuilder`, `UpdateBuilder`, `DeleteBuilder`), expression types, an attribute-driven model metadata system, and a SmartModel ORM layer.
2. **`Rymote.Radiant.Adapters.PostgreSql`**: PostgreSQL-specific adapter providing the dialect strings, identifier quoting, parameter formatting, value writer, and a Dapper-backed result mapper. Brings the `Npgsql` and `Dapper` package references.

## Solution layout

```
Rymote.Radiant.sln
├── Rymote.Radiant/                          — core (no DB driver dependency)
│   ├── Adapters/                            — IDatabaseAdapter contract + sub-dialects
│   ├── Sql/                                 — builders, clauses, expressions, compiler, executor
│   └── Smart/                               — SmartModel, SmartContext, SmartQuery, SmartRepository
├── Rymote.Radiant.Adapters.PostgreSql/      — PostgreSQL adapter implementation
├── Rymote.Radiant.Analyzers/                — Roslyn analyzers
├── Rymote.Radiant.Generators/               — Roslyn source generators
├── Rymote.Radiant.Tests/                    — xUnit tests
├── Playground/                              — manual end-to-end sample
└── docs/superpowers/
    ├── specs/2026-05-16-radiant-redesign-design.md
    └── plans/2026-05-16-radiant-redesign.md
```

## Public API entry points

### DI + adapter registration

```csharp
services.AddRadiant(builder =>
{
    builder.UsePostgreSql(connectionString, dataSourceBuilder =>
    {
        dataSourceBuilder.EnableDynamicJson();
    });
    builder.RegisterModelsFromAssembly(typeof(User).Assembly);
    builder.AddValueConverter<ContactId, string>(
        toDatabase: id => id.Value,
        fromDatabase: raw => new ContactId(raw));
    builder.AddGlobalQueryFilter(new MyTenantQueryFilter());
});
```

### Using a `SmartContext`

```csharp
await using SmartContext context = serviceProvider.GetRequiredService<SmartContext>();

User? user = await context.Query<User>()
    .Where(u => u.IsActive && u.Email == "alice@example.com")
    .FirstOrDefaultAsync();

await context.Repository<User>().InsertAsync(newUser);

await using ISmartTransaction transaction = await context.BeginTransactionAsync();
await context.Repository<Order>().InsertAsync(order);
await context.Repository<OrderItem>().InsertManyAsync(items);
await transaction.CommitAsync();
```

### Backwards-compatible static API

The legacy `SmartModel<T>.Query()` / `User.Query()` / `await user.SaveAsync()` calls still work. They route through:
1. `SmartContextAmbient.CurrentOrNull` (set via `SmartContextAmbient.Use(context)` or middleware) if available.
2. Otherwise the static `SmartModel.Configure(connection, cache)` if it was called.

For ASP.NET Core hosts, wire ambient context per request:

```csharp
app.Use(async (httpContext, next) =>
{
    SmartContext context = httpContext.RequestServices.GetRequiredService<SmartContext>();
    using (SmartContextAmbient.Use(context))
        await next(httpContext);
});
```

### `Include` / `ThenInclude`

Dot-notation `Include("Parent.Child")` is supported, and a typed chain via the `IncludeChain` extension method:

```csharp
await context.Query<Deal>()
    .IncludeChain(deal => deal.ContactRealEstateLink)
        .ThenInclude(link => link.Contact)
        .ThenInclude(contact => contact.Supervisor)
    .Where(deal => deal.Id == dealId)
    .FirstOrDefaultAsync();
```

### LINQ predicates supported

`LinqPredicateTranslator` handles:
- `==`, `!=`, `<`, `<=`, `>`, `>=`
- `&&`, `||`, `!`
- `string.Contains` / `StartsWith` / `EndsWith` → `LIKE`
- `Enumerable.Contains` → `IN`
- `IsActive` (boolean property access) → `column = TRUE`
- Comparisons against `null` → `IS NULL` / `IS NOT NULL`
- Captured locals and closures (resolved via reflection, never via JIT-compiled lambdas)

## Architecture notes

### `IDatabaseAdapter` and capabilities

`IDatabaseAdapter` exposes `ISqlDialect`, `IIdentifierQuoter`, `IParameterFormatter`, `IValueWriter`, `IResultMapper`, plus connection/command lifecycle. `DatabaseCapabilities` is a flags enum (`JsonbColumn`, `LateralJoin`, `ReturningClause`, `UpsertOnConflict`, etc.) that consumers check before using PostgreSQL-specific features.

### SqlEmitter — the canonical compile path

Every concrete `IQueryClause`, `IWhereExpression`, and `ISqlExpression` implements `Accept(SqlEmitter emitter)`. The emitter holds the active adapter and routes every emission decision through it:

- Identifiers: `emitter.WriteIdentifier(name)` → `Adapter.IdentifierQuoter.QuoteIdentifier`
- Qualified names: `emitter.WriteQualifiedName(schema, table)` → `Adapter.IdentifierQuoter.QuoteQualifiedName`
- Parameters: `emitter.WritePlaceholderForValue(value)` → adds via the dialect-formatted parameter name from `Adapter.ParameterFormatter`
- Keywords: `emitter.WriteKeyword(emitter.Dialect.Select)` etc. → `Adapter.Dialect.*`
- JSONB / array / vector / full-text / range operators → `Adapter.Dialect.Jsonb.*`, `Adapter.Dialect.Array.*`, `Adapter.Dialect.Vector.*`, `Adapter.Dialect.FullText.*`, `Adapter.Dialect.Range.*`

`QueryCompiler.Compile(builder, adapter)` is the canonical compile entry. Each `IQueryBuilder` implements `Build(IDatabaseAdapter adapter)` which calls into it.

The legacy `AppendTo`-based path and parameterless `Build()` are kept temporarily for callers that haven't been migrated, but every internal caller (SmartQuery, SmartRepository, SmartModel.FindAsync, RelationshipLoader) routes through `Build(adapter)` when an adapter is available.

### Adapter implementations

Two adapters ship today:

- **`Rymote.Radiant.Adapters.PostgreSql`** — full feature support: JSONB, arrays, vectors, full-text, ranges, RETURNING, ON CONFLICT, LATERAL joins, CTEs, window functions.
- **`Rymote.Radiant.Adapters.Sqlite`** — minimal proof-of-abstraction. Uses `$pN` parameter placeholders (different from Postgres' `@pN`), emits `1`/`0` for booleans, throws `NotSupportedException` from any dialect property the database doesn't support (e.g. `Vector.CosineDistanceOperator`). Verified end-to-end against an in-memory SQLite database via `SqliteAdapterTests`.

Adding another adapter (SqlServer, MySQL) is a focused additive change: implement the five interfaces (`ISqlDialect`, `IIdentifierQuoter`, `IParameterFormatter`, `IValueWriter`, `IResultMapper`) and an `IDatabaseAdapter`. Zero changes to core or to any clause/expression.

### SmartContext lifecycle

`SmartContext` is registered as scoped (`AddScoped`). It opens at most one `DbConnection` (lazy, on first use), and owns an optional ambient `DbTransaction`. Disposing the context disposes the connection and rolls back any open transaction. Both `IDisposable` and `IAsyncDisposable` are implemented.

### Transactions

`BeginTransactionAsync()` puts a `DbTransaction` on the context. All subsequent `Query<T>()`, `Repository<T>()`, and `Raw()` calls enlist on it automatically because `QueryExecutor` (and the raw command path) read it from the context. Disposing without committing rolls back.

### Value converters

Registered via `RadiantBuilder.AddValueConverter<TClr, TDatabase>(toDatabase, fromDatabase)`. Stored on `SmartContextOptions.ValueConverters`. Application of converters during INSERT/UPDATE/result-mapping is **not yet wired** — the infrastructure is in place but `SmartRepository.InsertAsync` and Dapper result mapping still operate on raw CLR values. This is a v2.1 item.

### Global query filters

Registered via `RadiantBuilder.AddGlobalQueryFilter(filter)`. When `SmartContext.Query<T>()` constructs a query, it iterates filters and applies any whose `MarkerInterface.IsAssignableFrom(typeof(T))` is true. Filters can call `query.Where(column, op, value)` to inject scoping conditions.

## Known gaps / v2.1 work

- **Value converter wiring at insert/update time and at result-mapping time.** Infrastructure exists; the SmartRepository and Dapper result mapper don't yet consult the registered converters.
- **Native PostgreSQL upsert (INSERT … ON CONFLICT DO UPDATE).** `UpsertAsync` currently routes to Insert-or-Update based on PK presence; the SQL builder has `OnConflictClause` available, just not wired here yet.
- **`Query.UpdateAsync(setterExpression)` and `Query.DeleteAsync()` bulk operations.** Not implemented; requires a `LinqToSetTranslator`.
- **`CancellationToken` overloads across the entire `ISmartQuery<T>` surface.** The repository surface is fully tokenised; the query surface keeps the legacy parameterless methods only.
- **Source-generated result mappers.** Generator stubs exist; the per-model mapper emit and `IResultMapper.RegisterSourceGenerated` aren't shipped.
- **Removing the legacy `AppendTo` path.** Every concrete clause and expression has `Accept(SqlEmitter)`, but `AppendTo` is still in place for backward compatibility with the parameterless `IQueryBuilder.Build()`. Removing both is a single mechanical sweep once external callers have migrated.
- **SqlServer / MySQL adapter projects.** Two adapters ship today (PostgreSQL, SQLite). Adding more is mechanical — one project per dialect, ~6 files each.
- **Aether migration guide.** The audit found Aether currently uses EF Core + Dapper; documenting the migration path is a separate piece of work.

## Common commands

```powershell
dotnet build Rymote.Radiant.sln
dotnet test  Rymote.Radiant.Tests/Rymote.Radiant.Tests.csproj
dotnet run --project Playground/Playground.csproj   # needs a live Postgres
```

## File-level conventions

- Fully descriptive identifiers; no acronyms (`databaseConnection`, not `db`; `cancellationToken`, not `ct`; `queryCommand`, not `cmd`).
- Comments are minimal — preserved only where the *why* is non-obvious. Doc comments are reserved for the public API surface in `Rymote.Radiant\Adapters\*.cs` and `Rymote.Radiant\Smart\Configuration\*.cs`.
- Async methods take `CancellationToken cancellationToken = default` wherever practical.
