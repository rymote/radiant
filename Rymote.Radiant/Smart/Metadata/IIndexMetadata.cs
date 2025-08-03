namespace Rymote.Radiant.Smart.Metadata;

public interface IIndexMetadata
{
    string? IndexName { get; }
    bool IsUnique { get; }
    bool IsClustered { get; }
}