using Npgsql;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Sql;

/// <summary>
/// Verifies the new adapter-aware compile path (walking <c>Accept(SqlEmitter)</c> through the
/// PostgreSQL adapter) produces SQL byte-identical to the legacy <c>AppendTo</c>-based path. If
/// these snapshots diverge, the migration of a specific clause/expression is wrong.
/// </summary>
public sealed class AdapterCompilePathTests
{
    private static IDatabaseAdapter PostgresAdapter { get; } = BuildOfflinePostgresAdapter();

    private static IDatabaseAdapter BuildOfflinePostgresAdapter()
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=offline;Database=offline");
        return new PostgreSqlAdapter(dataSourceBuilder.Build());
    }

    [Fact]
    public void SimpleSelectMatchesLegacy()
    {
        SelectBuilder Build() => new SelectBuilder()
            .Select(new ColumnExpression("id"), new ColumnExpression("email"))
            .From("users")
            .Where("id", "=", 1)
            .OrderBy("created_at");

        Assert.Equal(Build().Build().SqlText, Build().Build(PostgresAdapter).SqlText);
    }

    [Fact]
    public void DistinctSelectMatchesLegacy()
    {
        SelectBuilder Build() => new SelectBuilder()
            .SelectDistinct(new ColumnExpression("email"))
            .From("users");

        Assert.Equal(Build().Build().SqlText, Build().Build(PostgresAdapter).SqlText);
    }

    [Fact]
    public void InsertWithReturningMatchesLegacy()
    {
        InsertBuilder Build() => new InsertBuilder()
            .Into("users")
            .Value("email", "alice@example.com")
            .Value("username", "alice")
            .Returning("id");

        Assert.Equal(Build().Build().SqlText, Build().Build(PostgresAdapter).SqlText);
    }

    [Fact]
    public void UpdateMatchesLegacy()
    {
        UpdateBuilder Build() => new UpdateBuilder()
            .Table("users")
            .Set("username", "bob")
            .Where("id", "=", 5);

        Assert.Equal(Build().Build().SqlText, Build().Build(PostgresAdapter).SqlText);
    }

    [Fact]
    public void DeleteMatchesLegacy()
    {
        DeleteBuilder Build() => new DeleteBuilder()
            .From("users")
            .Where("id", "=", 7);

        Assert.Equal(Build().Build().SqlText, Build().Build(PostgresAdapter).SqlText);
    }

    [Fact]
    public void JsonbContainsRoutesThroughDialect()
    {
        SelectBuilder Build() => new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("contacts")
            .WhereBooleanExpression(new JsonbExpression(
                new ColumnExpression("emails"),
                JsonbOperator.StrictContains,
                new LiteralExpression("[\"a@b.c\"]")));

        string adapterSql = Build().Build(PostgresAdapter).SqlText;
        Assert.Contains("@>", adapterSql);
        Assert.Contains("\"emails\"", adapterSql);
    }

    [Fact]
    public void AdapterPathUsesAdapterIdentifierQuoter()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("users");

        string sql = selectBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("\"users\"", sql);
        Assert.Contains("\"id\"", sql);
    }
}
