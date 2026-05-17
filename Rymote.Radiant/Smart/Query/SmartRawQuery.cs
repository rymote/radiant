using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Rymote.Radiant.Smart.Context;

namespace Rymote.Radiant.Smart.Query;

public sealed class SmartRawQuery : ISmartRawQuery
{
    private readonly IDbConnection _databaseConnection;
    private readonly SmartContext? _smartContext;

    public SmartRawQuery(IDbConnection databaseConnection)
        : this(databaseConnection, smartContext: null)
    {
    }

    public SmartRawQuery(IDbConnection databaseConnection, SmartContext? smartContext)
    {
        _databaseConnection = databaseConnection;
        _smartContext = smartContext;
    }

    public async Task<List<TResult>> QueryAsync<TResult>(string sql, object? parameters = null)
        => await QueryAsync<TResult>(sql, parameters, CancellationToken.None);

    public async Task<List<TResult>> QueryAsync<TResult>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        CommandDefinition command = BuildCommand(sql, parameters, cancellationToken);
        IEnumerable<TResult> results = await _databaseConnection.QueryAsync<TResult>(command);
        return results.ToList();
    }

    public async Task<TResult> QuerySingleAsync<TResult>(string sql, object? parameters = null)
        => await QuerySingleAsync<TResult>(sql, parameters, CancellationToken.None);

    public async Task<TResult> QuerySingleAsync<TResult>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        CommandDefinition command = BuildCommand(sql, parameters, cancellationToken);
        return await _databaseConnection.QuerySingleAsync<TResult>(command);
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(string sql, object? parameters = null)
        => await QuerySingleOrDefaultAsync<TResult>(sql, parameters, CancellationToken.None);

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(string sql, object? parameters, CancellationToken cancellationToken)
    {
        CommandDefinition command = BuildCommand(sql, parameters, cancellationToken);
        return await _databaseConnection.QuerySingleOrDefaultAsync<TResult?>(command);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        => await ExecuteAsync(sql, parameters, CancellationToken.None);

    public async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
    {
        CommandDefinition command = BuildCommand(sql, parameters, cancellationToken);
        return await _databaseConnection.ExecuteAsync(command);
    }

    private CommandDefinition BuildCommand(string sql, object? parameters, CancellationToken cancellationToken)
        => new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            transaction: _smartContext?.AmbientTransaction,
            cancellationToken: cancellationToken);
}
