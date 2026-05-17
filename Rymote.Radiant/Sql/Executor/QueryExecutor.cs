using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Rymote.Radiant.Sql.Executor;

public sealed class QueryExecutor
{
    private readonly IDbConnection databaseConnection;
    private readonly IDbTransaction? activeTransaction;

    public QueryExecutor(IDbConnection databaseConnection, IDbTransaction? activeTransaction = null)
    {
        this.databaseConnection = databaseConnection;
        this.activeTransaction = activeTransaction;
    }

    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        return await databaseConnection.QueryAsync<TResult>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<TResult> QuerySingleAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        return await databaseConnection.QuerySingleAsync<TResult>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        return await databaseConnection.QuerySingleOrDefaultAsync<TResult?>(BuildCommand(queryCommand, cancellationToken));
    }

    public async Task<int> ExecuteAsync(QueryCommand queryCommand, CancellationToken cancellationToken = default)
    {
        return await databaseConnection.ExecuteAsync(BuildCommand(queryCommand, cancellationToken));
    }

    private CommandDefinition BuildCommand(QueryCommand queryCommand, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText: queryCommand.SqlText,
            parameters: queryCommand.Parameters,
            transaction: activeTransaction,
            cancellationToken: cancellationToken);
    }
}
