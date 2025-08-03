using System;
using System.Data;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class SmartModelConfiguration : ISmartModelConfiguration
{
    private IDbConnection? databaseConnection;
    private readonly IModelMetadataCache modelMetadataCache;

    public SmartModelConfiguration()
    {
        IModelMetadataScanner scanner = new ModelMetadataScanner();
        this.modelMetadataCache = new ModelMetadataCache(scanner);
    }

    public ISmartModelConfiguration UseConnection(IDbConnection connection)
    {
        this.databaseConnection = connection;
        return this;
    }

    public ISmartModelConfiguration RegisterModel<TModel>() where TModel : class
    {
        modelMetadataCache.RegisterModel<TModel>();
        return this;
    }

    public ISmartModelConfiguration RegisterModel(Type modelType)
    {
        modelMetadataCache.RegisterModel(modelType);
        return this;
    }

    public void Build()
    {
        if (databaseConnection == null)
        {
            throw new InvalidOperationException("Database connection must be configured");
        }

        SmartModel.Configure(databaseConnection, modelMetadataCache);
    }
    
    public IModelMetadataCache GetModelMetadataCache()
    {
        return modelMetadataCache;
    }
}