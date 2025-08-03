namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public sealed class IndexAttribute : Attribute
{
    public string? IndexName { get; }
    public bool IsUnique { get; }
    public bool IsClustered { get; }

    public IndexAttribute(string? indexName = null, bool isUnique = false, bool isClustered = false)
    {
        IndexName = indexName;
        IsUnique = isUnique;
        IsClustered = isClustered;
    }
}