namespace Rymote.Radiant.Smart.Metadata;

public interface IModelMetadataCache
{
    IModelMetadata GetMetadata(Type modelType);
    IModelMetadata GetMetadata<TModel>() where TModel : class;
    void RegisterModel(Type modelType);
    void RegisterModel<TModel>() where TModel : class;
}