using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.MySql;

public sealed class MySqlAdapter : IDatabaseAdapter
{
    private readonly string connectionString;

    public MySqlAdapter(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public string Identifier => "mysql";

    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.CommonTableExpression
        | DatabaseCapabilities.RecursiveCommonTableExpression
        | DatabaseCapabilities.WindowFunctions
        | DatabaseCapabilities.SchemaPerTable
        | DatabaseCapabilities.CaseInsensitiveLikeOperator
        | DatabaseCapabilities.FullTextSearch;

    public ISqlDialect Dialect { get; } = new MySqlDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new MySqlIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new MySqlParameterFormatter();
    public IValueWriter ValueWriter { get; } = new MySqlValueWriter();
    public IResultMapper ResultMapper { get; } = new DapperMySqlResultMapper();

    public DbConnection CreateConnection() => new MySqlConnection(connectionString);

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        MySqlConnection connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery)
    {
        MySqlCommand command = (MySqlCommand)connection.CreateCommand();
        command.CommandText = compiledQuery.Sql;
        foreach (QueryParameter parameter in compiledQuery.Parameters)
        {
            MySqlParameter mysqlParameter = (MySqlParameter)command.CreateParameter();
            mysqlParameter.ParameterName = parameter.Name;
            mysqlParameter.Value = parameter.Value ?? System.DBNull.Value;
            if (parameter.Type.HasValue)
                mysqlParameter.DbType = parameter.Type.Value;
            command.Parameters.Add(mysqlParameter);
        }
        return command;
    }
}
