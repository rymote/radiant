using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Smart.Context;

internal sealed class SmartTransaction : ISmartTransaction
{
    private readonly SmartContext owningContext;
    private readonly DbTransaction underlyingTransaction;
    private bool isCompleted;

    internal SmartTransaction(SmartContext owningContext, DbTransaction underlyingTransaction)
    {
        this.owningContext = owningContext;
        this.underlyingTransaction = underlyingTransaction;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (isCompleted) return;
        await underlyingTransaction.CommitAsync(cancellationToken);
        isCompleted = true;
        owningContext.ClearAmbientTransaction();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (isCompleted) return;
        await underlyingTransaction.RollbackAsync(cancellationToken);
        isCompleted = true;
        owningContext.ClearAmbientTransaction();
    }

    public async ValueTask DisposeAsync()
    {
        if (!isCompleted)
        {
            try { await underlyingTransaction.RollbackAsync(); }
            catch { /* swallow — rollback during dispose is best-effort. */ }
            owningContext.ClearAmbientTransaction();
            isCompleted = true;
        }
        await underlyingTransaction.DisposeAsync();
    }
}
