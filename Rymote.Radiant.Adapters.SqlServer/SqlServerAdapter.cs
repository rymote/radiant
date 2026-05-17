using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.SqlServer;

public sealed class SqlServerAdapter : IDatabaseAdapter
{
    private readonly string connectionString;

    public SqlServerAdapter(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public string Identifier => "sqlserver";

    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.ReturningClause
        | DatabaseCapabilities.CommonTableExpression
        | DatabaseCapabilities.RecursiveCommonTableExpression
        | DatabaseCapabilities.WindowFunctions
        | DatabaseCapabilities.SchemaPerTable
        | DatabaseCapabilities.BatchedInsertReturning;

    public ISqlDialect Dialect { get; } = new SqlServerDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new SqlServerIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new SqlServerParameterFormatter();
    public IValueWriter ValueWriter { get; } = new SqlServerValueWriter();
    public IResultMapper ResultMapper { get; } = new DapperSqlServerResultMapper();

    public DbConnection CreateConnection() => new SqlConnection(connectionString);

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery)
    {
        SqlCommand command = (SqlCommand)connection.CreateCommand();
        command.CommandText = compiledQuery.Sql;
        foreach (QueryParameter parameter in compiledQuery.Parameters)
        {
            SqlParameter sqlParameter = command.CreateParameter();
            sqlParameter.ParameterName = parameter.Name;
            sqlParameter.Value = parameter.Value ?? System.DBNull.Value;
            if (parameter.Type.HasValue)
                sqlParameter.DbType = parameter.Type.Value;
            command.Parameters.Add(sqlParameter);
        }
        return command;
    }
}
