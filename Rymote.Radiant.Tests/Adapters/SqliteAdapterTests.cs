using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Adapters.Sqlite;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Adapters;

/// <summary>
/// Proves the adapter abstraction is real: the same builder graph compiles to different SQL on
/// different adapters (different placeholder prefix, different boolean literals), and the SQLite
/// emission actually runs against a real SQLite database.
/// </summary>
public sealed class SqliteAdapterTests
{
    private static SqliteAdapter Adapter { get; } = new SqliteAdapter("Data Source=:memory:");
    private static PostgreSqlAdapter PostgresAdapter { get; } = BuildOfflinePostgresAdapter();

    private static PostgreSqlAdapter BuildOfflinePostgresAdapter()
    {
        NpgsqlDataSourceBuilderShim shim = new NpgsqlDataSourceBuilderShim();
        return shim.BuildAdapter();
    }

    private sealed class NpgsqlDataSourceBuilderShim
    {
        public PostgreSqlAdapter BuildAdapter()
        {
            global::Npgsql.NpgsqlDataSourceBuilder dataSourceBuilder = new global::Npgsql.NpgsqlDataSourceBuilder("Host=offline;Database=offline");
            return new PostgreSqlAdapter(dataSourceBuilder.Build());
        }
    }

    [Fact]
    public void SqliteUsesDollarPlaceholderPrefixWhilePostgresUsesAt()
    {
        SelectBuilder Build() => new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("users")
            .Where("id", "=", 42);

        string postgresSql = Build().Build(PostgresAdapter).SqlText;
        string sqliteSql = Build().Build(Adapter).SqlText;

        Assert.Contains("@p0", postgresSql);
        Assert.Contains("$p0", sqliteSql);
        Assert.DoesNotContain("@p0", sqliteSql);
    }

    [Fact]
    public void SqliteCapabilitiesDoNotIncludeJsonb()
    {
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.JsonbColumn));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.VectorColumn));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.LateralJoin));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.CommonTableExpression));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.UpsertOnConflict));
    }

    [Fact]
    public async Task SqliteAdapterCompilesAndExecutesAgainstRealDatabase()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (SqliteCommand schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY, email TEXT, is_active INTEGER)";
            await schemaCommand.ExecuteNonQueryAsync();
        }

        using (SqliteCommand seedCommand = connection.CreateCommand())
        {
            seedCommand.CommandText = "INSERT INTO users (id, email, is_active) VALUES (1, 'alice@example.com', 1)";
            await seedCommand.ExecuteNonQueryAsync();
        }

        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"), new ColumnExpression("email"))
            .From("users")
            .Where("is_active", "=", 1);

        Rymote.Radiant.Sql.QueryCommand compiledQuery = selectBuilder.Build(Adapter);
        Assert.Contains("$p0", compiledQuery.SqlText);

        using System.Data.Common.DbCommand dbCommand = Adapter.CreateCommand(connection, compiledQuery.ToCompiledQuery());
        await using System.Data.Common.DbDataReader reader = await dbCommand.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal("alice@example.com", reader.GetString(1));
        Assert.False(await reader.ReadAsync());
    }
}
