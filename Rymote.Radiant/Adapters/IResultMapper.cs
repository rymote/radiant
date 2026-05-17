using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Adapters;

public interface IResultMapper
{
    Task<IReadOnlyList<TResult>> QueryAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<TResult> QuerySingleAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(DbCommand command, CancellationToken cancellationToken);
    Task<int> ExecuteAsync(DbCommand command, CancellationToken cancellationToken);
}
