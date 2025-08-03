namespace Rymote.Radiant.Smart.Metadata;

public sealed class IndexMetadata : IIndexMetadata
{
    public string? IndexName { get; }
    public bool IsUnique { get; }
    public bool IsClustered { get; }

    public IndexMetadata(string? indexName, bool isUnique, bool isClustered)
    {
        IndexName = indexName;
        IsUnique = isUnique;
        IsClustered = isClustered;
    }
}