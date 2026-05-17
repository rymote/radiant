using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Context;

public interface ISmartTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
