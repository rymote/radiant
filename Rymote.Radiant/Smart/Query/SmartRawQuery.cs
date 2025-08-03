using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Rymote.Radiant.Smart.Query;

public sealed class SmartRawQuery : ISmartRawQuery
{
    private readonly IDbConnection databaseConnection;

    public SmartRawQuery(IDbConnection databaseConnection)
    {
        this.databaseConnection = databaseConnection;
    }

    public async Task<List<TResult>> QueryAsync<TResult>(string sql, object? parameters = null)
    {
        IEnumerable<TResult> results = await databaseConnection.QueryAsync<TResult>(sql, parameters);
        return results.ToList();
    }

    public async Task<TResult> QuerySingleAsync<TResult>(string sql, object? parameters = null)
    {
        return await databaseConnection.QuerySingleAsync<TResult>(sql, parameters);
    }

    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(string sql, object? parameters = null)
    {
        return await databaseConnection.QuerySingleOrDefaultAsync<TResult?>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        return await databaseConnection.ExecuteAsync(sql, parameters);
    }
} 