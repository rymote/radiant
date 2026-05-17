using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Smart.Query;
using Rymote.Radiant.Smart.Repository;

namespace Rymote.Radiant.Smart.Context;

public sealed class SmartContext : IAsyncDisposable, IDisposable
{
    private readonly SmartContextOptions options;
    private readonly IServiceProvider serviceProvider;
    private DbConnection? openConnection;
    private DbTransaction? ambientTransaction;
    private bool ownsConnection;

    public SmartContext(SmartContextOptions options, IServiceProvider serviceProvider)
    {
        this.options = options;
        this.serviceProvider = serviceProvider;
    }

    public IDatabaseAdapter Adapter => options.Adapter;
    public IModelMetadataCache MetadataCache => options.ModelMetadataCache;
    public SmartContextOptions Options => options;
    public IServiceProvider Services => serviceProvider;
    public DbTransaction? AmbientTransaction => ambientTransaction;
    public string? SchemaOverride => options.SchemaOverride;

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (openConnection is { State: ConnectionState.Open })
            return openConnection;

        openConnection = await options.Adapter.OpenConnectionAsync(cancellationToken);
        ownsConnection = true;
        return openConnection;
    }

    public ISmartQuery<TModel> Query<TModel>() where TModel : class, new()
    {
        DbConnection connection = GetOpenConnectionAsync().GetAwaiter().GetResult();
        IModelMetadata metadata = options.ModelMetadataCache.GetMetadata<TModel>();
        SmartQuery<TModel> query = new SmartQuery<TModel>(connection, metadata, this);
        if (!string.IsNullOrWhiteSpace(options.SchemaOverride))
            query.Schema(options.SchemaOverride!);
        foreach (GlobalQueryFilter filter in options.GlobalQueryFilters)
        {
            if (filter.MarkerInterface.IsAssignableFrom(typeof(TModel)))
                filter.Apply(query, serviceProvider);
        }
        return query;
    }

    public ISmartRepository<TModel> Repository<TModel>() where TModel : class, new()
    {
        DbConnection connection = GetOpenConnectionAsync().GetAwaiter().GetResult();
        IModelMetadata metadata = options.ModelMetadataCache.GetMetadata<TModel>();
        return new SmartRepository<TModel>(connection, metadata, this);
    }

    public ISmartRawQuery Raw()
    {
        DbConnection connection = GetOpenConnectionAsync().GetAwaiter().GetResult();
        return new SmartRawQuery(connection, this);
    }

    public async Task<ISmartTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (ambientTransaction is not null)
            throw new InvalidOperationException("A transaction is already active on this SmartContext.");

        DbConnection connection = await GetOpenConnectionAsync(cancellationToken);
        ambientTransaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new SmartTransaction(this, ambientTransaction);
    }

    public SmartContext WithSchema(string schemaName)
        => new SmartContext(options.WithSchema(schemaName), serviceProvider);

    internal void ClearAmbientTransaction() => ambientTransaction = null;

    public async ValueTask DisposeAsync()
    {
        if (ambientTransaction is not null)
        {
            try { await ambientTransaction.RollbackAsync(); } catch { /* best effort */ }
            await ambientTransaction.DisposeAsync();
            ambientTransaction = null;
        }
        if (ownsConnection && openConnection is not null)
        {
            await openConnection.DisposeAsync();
            openConnection = null;
        }
    }

    public void Dispose()
    {
        if (ambientTransaction is not null)
        {
            try { ambientTransaction.Rollback(); } catch { /* best effort */ }
            ambientTransaction.Dispose();
            ambientTransaction = null;
        }
        if (ownsConnection && openConnection is not null)
        {
            openConnection.Dispose();
            openConnection = null;
        }
    }
}
