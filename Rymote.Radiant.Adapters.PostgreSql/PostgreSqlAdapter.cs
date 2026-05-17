using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlAdapter : IDatabaseAdapter
{
    private readonly NpgsqlDataSource dataSource;

    public PostgreSqlAdapter(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public string Identifier => "postgresql";

    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.ReturningClause
        | DatabaseCapabilities.UpsertOnConflict
        | DatabaseCapabilities.CommonTableExpression
        | DatabaseCapabilities.RecursiveCommonTableExpression
        | DatabaseCapabilities.LateralJoin
        | DatabaseCapabilities.WindowFunctions
        | DatabaseCapabilities.SchemaPerTable
        | DatabaseCapabilities.JsonbColumn
        | DatabaseCapabilities.ArrayColumn
        | DatabaseCapabilities.VectorColumn
        | DatabaseCapabilities.FullTextSearch
        | DatabaseCapabilities.RangeTypes
        | DatabaseCapabilities.CaseInsensitiveText
        | DatabaseCapabilities.CaseInsensitiveLikeOperator
        | DatabaseCapabilities.RegularExpressionOperator
        | DatabaseCapabilities.NamedSequences
        | DatabaseCapabilities.BatchedInsertReturning;

    public ISqlDialect Dialect { get; } = new PostgreSqlDialect();
    public IIdentifierQuoter IdentifierQuoter { get; } = new PostgreSqlIdentifierQuoter();
    public IParameterFormatter ParameterFormatter { get; } = new PostgreSqlParameterFormatter();
    public IValueWriter ValueWriter { get; } = new PostgreSqlValueWriter();
    public IResultMapper ResultMapper { get; } = new DapperResultMapper();

    public DbConnection CreateConnection() => dataSource.CreateConnection();

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = dataSource.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery)
    {
        NpgsqlCommand command = (NpgsqlCommand)connection.CreateCommand();
        command.CommandText = compiledQuery.Sql;
        foreach (QueryParameter parameter in compiledQuery.Parameters)
        {
            NpgsqlParameter npgsqlParameter = (NpgsqlParameter)command.CreateParameter();
            npgsqlParameter.ParameterName = parameter.Name;
            npgsqlParameter.Value = parameter.Value ?? System.DBNull.Value;
            if (parameter.Type.HasValue)
                npgsqlParameter.DbType = parameter.Type.Value;
            command.Parameters.Add(npgsqlParameter);
        }
        return command;
    }
}
