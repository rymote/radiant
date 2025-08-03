using System.Collections.Concurrent;

namespace Rymote.Radiant.Smart.Metadata;

public sealed class ModelMetadataCache : IModelMetadataCache
{
    private readonly ConcurrentDictionary<Type, IModelMetadata> metadataCache;
    private readonly IModelMetadataScanner modelMetadataScanner;

    public ModelMetadataCache(IModelMetadataScanner modelMetadataScanner)
    {
        this.metadataCache = new ConcurrentDictionary<Type, IModelMetadata>();
        this.modelMetadataScanner = modelMetadataScanner;
    }

    public IModelMetadata GetMetadata(Type modelType)
    {
        return metadataCache.GetOrAdd(modelType, type => modelMetadataScanner.ScanModel(type));
    }

    public IModelMetadata GetMetadata<TModel>() where TModel : class
    {
        return GetMetadata(typeof(TModel));
    }

    public void RegisterModel(Type modelType)
    {
        if (metadataCache.ContainsKey(modelType)) return;
        
        IModelMetadata metadata = modelMetadataScanner.ScanModel(modelType);
        metadataCache.TryAdd(modelType, metadata);
    }

    public void RegisterModel<TModel>() where TModel : class
    {
        RegisterModel(typeof(TModel));
    }
}