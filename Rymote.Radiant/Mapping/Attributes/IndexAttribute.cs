namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class IndexAttribute : Attribute
{
    public string? IndexName { get; }
    public bool IsUnique { get; }

    public IndexAttribute(string? indexName = null, bool isUnique = false)
    {
        IndexName = indexName;
        IsUnique = isUnique;
    }
}