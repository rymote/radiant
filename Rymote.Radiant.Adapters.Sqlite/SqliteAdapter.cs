using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.Sqlite;

public sealed class SqliteAdapter : IDatabaseAdapter
{
    private readonly string connectionString;

    public SqliteAdapter(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public string Identifier => "sqlite";

    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.ReturningClause
        | DatabaseCapabilities.UpsertOnConflict
        | DatabaseCapabilities.CommonTableExpression
        | DatabaseCapabilities.RecursiveCommonTableExpression
        | DatabaseCapabilities.WindowFunctions;

    public ISqlDialect Dialect { get; } = new SqliteDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new SqliteIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new SqliteParameterFormatter();
    public IValueWriter ValueWriter { get; } = new SqliteValueWriter();
    public IResultMapper ResultMapper { get; } = new DapperSqliteResultMapper();

    public DbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        SqliteConnection connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery)
    {
        SqliteCommand command = (SqliteCommand)connection.CreateCommand();
        command.CommandText = compiledQuery.Sql;
        foreach (QueryParameter parameter in compiledQuery.Parameters)
        {
            SqliteParameter sqliteParameter = command.CreateParameter();
            // SQLite expects "$" prefix in CommandText but parameter name without prefix.
            sqliteParameter.ParameterName = "$" + parameter.Name;
            sqliteParameter.Value = parameter.Value ?? System.DBNull.Value;
            command.Parameters.Add(sqliteParameter);
        }
        return command;
    }
}
