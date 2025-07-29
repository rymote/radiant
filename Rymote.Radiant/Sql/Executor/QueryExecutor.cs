using System.Data;
using Dapper;

namespace Rymote.Radiant.Sql.Executor;

public sealed class QueryExecutor
{
    private readonly IDbConnection databaseConnection;

    public QueryExecutor(IDbConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(QueryCommand queryCommand)
    {
        return await databaseConnection.QueryAsync<T>(queryCommand.SqlText, queryCommand.Parameters);
    }

    public async Task<T> QuerySingleAsync<T>(QueryCommand queryCommand)
    {
        return await databaseConnection.QuerySingleAsync<T>(queryCommand.SqlText, queryCommand.Parameters);
    }

    public async Task<int> ExecuteAsync(QueryCommand queryCommand)
    {
        return await databaseConnection.ExecuteAsync(queryCommand.SqlText, queryCommand.Parameters);
    }
}
