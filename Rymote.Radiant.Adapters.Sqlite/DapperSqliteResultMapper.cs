using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.Sqlite;

public sealed class DapperSqliteResultMapper : IResultMapper
{
    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
    {
        IEnumerable<TResult> results = await command.Connection!.QueryAsync<TResult>(BuildCommand(command, cancellationToken));
        return results.ToList();
    }

    public Task<TResult> QuerySingleAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.QuerySingleAsync<TResult>(BuildCommand(command, cancellationToken));

    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.QuerySingleOrDefaultAsync<TResult?>(BuildCommand(command, cancellationToken));

    public Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken)
        => command.Connection!.ExecuteAsync(BuildCommand(command, cancellationToken));

    private static CommandDefinition BuildCommand(DbCommand source, CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new DynamicParameters();
        foreach (DbParameter parameter in source.Parameters)
            parameters.Add(parameter.ParameterName, parameter.Value);

        return new CommandDefinition(
            commandText: source.CommandText,
            parameters: parameters,
            transaction: source.Transaction,
            commandTimeout: source.CommandTimeout,
            commandType: source.CommandType,
            cancellationToken: cancellationToken);
    }
}
