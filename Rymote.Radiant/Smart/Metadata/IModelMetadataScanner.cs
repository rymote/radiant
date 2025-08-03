namespace Rymote.Radiant.Smart.Metadata;

public interface IModelMetadataScanner
{
    IModelMetadata ScanModel(Type modelType);
}