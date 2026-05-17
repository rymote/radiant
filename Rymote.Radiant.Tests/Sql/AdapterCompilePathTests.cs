using Npgsql;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Sql;

/// <summary>
/// Verifies the adapter-aware compile path (walking <c>Accept(SqlEmitter)</c> through the
/// PostgreSQL adapter) produces the expected SQL surface for common builder shapes.
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
    public void SimpleSelectEmitsExpectedSql()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"), new ColumnExpression("email"))
            .From("users")
            .Where("id", "=", 1)
            .OrderBy("created_at");

        string sql = selectBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("\"users\"", sql);
        Assert.Contains("\"id\"", sql);
        Assert.Contains("\"email\"", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void DistinctSelectEmitsDistinctKeyword()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .SelectDistinct(new ColumnExpression("email"))
            .From("users");

        string sql = selectBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("DISTINCT", sql);
        Assert.Contains("\"email\"", sql);
    }

    [Fact]
    public void InsertWithReturningEmitsReturningClause()
    {
        InsertBuilder insertBuilder = new InsertBuilder()
            .Into("users")
            .Value("email", "alice@example.com")
            .Value("username", "alice")
            .Returning("id");

        string sql = insertBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("RETURNING", sql);
        Assert.Contains("\"id\"", sql);
    }

    [Fact]
    public void UpdateEmitsSetClause()
    {
        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table("users")
            .Set("username", "bob")
            .Where("id", "=", 5);

        string sql = updateBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
        Assert.Contains("\"username\"", sql);
    }

    [Fact]
    public void DeleteEmitsDeleteFromClause()
    {
        DeleteBuilder deleteBuilder = new DeleteBuilder()
            .From("users")
            .Where("id", "=", 7);

        string sql = deleteBuilder.Build(PostgresAdapter).SqlText;

        Assert.Contains("DELETE", sql);
        Assert.Contains("\"users\"", sql);
    }

    [Fact]
    public void JsonbContainsRoutesThroughDialect()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("contacts")
            .WhereBooleanExpression(new JsonbExpression(
                new ColumnExpression("emails"),
                JsonbOperator.StrictContains,
                new LiteralExpression("[\"a@b.c\"]")));

        string adapterSql = selectBuilder.Build(PostgresAdapter).SqlText;
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
