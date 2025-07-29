namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class BelongsToAttribute : Attribute
{
    public string ForeignKey { get; }
    public string JoinTable { get; }
    public string RelatedKey { get; }
    public bool IsManyToMany => !string.IsNullOrEmpty(JoinTable);

    public BelongsToAttribute(string foreignKey)
    {
        ForeignKey = foreignKey;
    }

    public BelongsToAttribute(string joinTable, string foreignKey, string relatedKey)
    {
        JoinTable = joinTable;
        ForeignKey = foreignKey;
        RelatedKey = relatedKey;
    }
}