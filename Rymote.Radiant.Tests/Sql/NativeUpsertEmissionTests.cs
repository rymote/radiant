using Npgsql;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Adapters.Sqlite;
using Rymote.Radiant.Sql.Builder;
using Xunit;

namespace Rymote.Radiant.Tests.Sql;

/// <summary>
/// Verifies that <c>InsertBuilder.OnConflictDoUpdateFromExcluded(...)</c> emits the correct
/// adapter-specific SQL — PostgreSQL uses <c>EXCLUDED</c>, SQLite uses <c>excluded</c>.
/// </summary>
public sealed class NativeUpsertEmissionTests
{
    private static IDatabaseAdapter PostgresAdapter { get; } = new PostgreSqlAdapter(
        new NpgsqlDataSourceBuilder("Host=offline;Database=offline").Build());

    private static IDatabaseAdapter SqliteAdapter { get; } = new SqliteAdapter("Data Source=:memory:");

    [Fact]
    public void PostgresEmitsExcludedKeywordInUpperCase()
    {
        InsertBuilder builder = new InsertBuilder()
            .Into("users")
            .Value("email", "alice@example.com")
            .Value("username", "alice")
            .OnConflictDoUpdateFromExcluded(
                conflictColumns: new[] { "id" },
                columnsToUpdateFromExcluded: new[] { "email", "username" })
            .Returning("id", "email", "username");

        string sql = builder.Build(PostgresAdapter).SqlText;
        Assert.Contains("ON CONFLICT", sql);
        Assert.Contains("DO UPDATE", sql);
        Assert.Contains("\"email\" = EXCLUDED.\"email\"", sql);
        Assert.Contains("\"username\" = EXCLUDED.\"username\"", sql);
        Assert.Contains("RETURNING", sql);
    }

    [Fact]
    public void SqliteEmitsExcludedKeywordInLowerCase()
    {
        InsertBuilder builder = new InsertBuilder()
            .Into("users")
            .Value("email", "alice@example.com")
            .Value("username", "alice")
            .OnConflictDoUpdateFromExcluded(
                conflictColumns: new[] { "id" },
                columnsToUpdateFromExcluded: new[] { "email", "username" });

        string sql = builder.Build(SqliteAdapter).SqlText;
        Assert.Contains("\"email\" = excluded.\"email\"", sql);
        Assert.Contains("\"username\" = excluded.\"username\"", sql);
    }

    [Fact]
    public void NativeUpsertConflictColumnIsQuotedThroughAdapter()
    {
        InsertBuilder builder = new InsertBuilder()
            .Into("users")
            .Value("email", "alice@example.com")
            .OnConflictDoUpdateFromExcluded(
                conflictColumns: new[] { "email" },
                columnsToUpdateFromExcluded: new[] { "email" });

        string postgresSql = builder.Build(PostgresAdapter).SqlText;
        Assert.Contains("ON CONFLICT (\"email\")", postgresSql);

        string sqliteSql = builder.Build(SqliteAdapter).SqlText;
        Assert.Contains("ON CONFLICT (\"email\")", sqliteSql);
    }
}
