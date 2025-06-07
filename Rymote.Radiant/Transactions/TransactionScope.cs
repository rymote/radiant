using System.Data;

namespace Rymote.Radiant.Transactions;

public class TransactionScope : IDisposable, IAsyncDisposable
{
    private readonly IDbTransaction transaction;
    private bool committed = false;
    private bool disposed = false;

    public TransactionScope(IDbTransaction transaction)
    {
        this.transaction = transaction;
    }

    public void Commit()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(TransactionScope));
        
        transaction.Commit();
        committed = true;
    }

    public async Task CommitAsync()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(TransactionScope));
        
        if (transaction is IAsyncDisposable asyncTransaction)
        {
            await asyncTransaction.DisposeAsync();
        }
        else
        {
            transaction.Commit();
        }
        committed = true;
    }

    public void Rollback()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(TransactionScope));
        
        transaction.Rollback();
    }

    public void Dispose()
    {
        if (disposed)
            return;

        if (!committed)
        {
            transaction.Rollback();
        }

        transaction.Dispose();
        disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        if (!committed)
        {
            transaction.Rollback();
        }

        if (transaction is IAsyncDisposable asyncTransaction)
        {
            await asyncTransaction.DisposeAsync();
        }
        else
        {
            transaction.Dispose();
        }
        
        disposed = true;
    }
}
