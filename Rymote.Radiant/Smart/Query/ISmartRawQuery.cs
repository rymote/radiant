using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Query;

public interface ISmartRawQuery
{
    Task<List<TResult>> QueryAsync<TResult>(string sql, object? parameters = null);
    Task<TResult> QuerySingleAsync<TResult>(string sql, object? parameters = null);
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(string sql, object? parameters = null);
    Task<int> ExecuteAsync(string sql, object? parameters = null);
} 