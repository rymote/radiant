using System;
using System.Data;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class SmartModelConfiguration : ISmartModelConfiguration
{
    private IDbConnection? _databaseConnection;
    private IDatabaseAdapter? _databaseAdapter;
    private readonly IModelMetadataCache _modelMetadataCache;

    public SmartModelConfiguration()
    {
        IModelMetadataScanner scanner = new ModelMetadataScanner();
        _modelMetadataCache = new ModelMetadataCache(scanner);
    }

    public ISmartModelConfiguration UseAdapter(IDatabaseAdapter adapter)
    {
        _databaseAdapter = adapter;
        return this;
    }

    public ISmartModelConfiguration UseConnection(IDbConnection connection)
    {
        _databaseConnection = connection;
        return this;
    }

    public ISmartModelConfiguration RegisterModel<TModel>() where TModel : class
    {
        _modelMetadataCache.RegisterModel<TModel>();
        return this;
    }

    public ISmartModelConfiguration RegisterModel(Type modelType)
    {
        _modelMetadataCache.RegisterModel(modelType);
        return this;
    }

    public void Build()
    {
        if (_databaseConnection == null)
            throw new InvalidOperationException("Database connection must be configured");

        if (_databaseAdapter == null)
            throw new InvalidOperationException(
                "Database adapter must be configured. Call UseAdapter(...) before Build().");

        SmartModel.Configure(_databaseAdapter, _databaseConnection, _modelMetadataCache);
    }

    public IModelMetadataCache GetModelMetadataCache()
    {
        return _modelMetadataCache;
    }
}
