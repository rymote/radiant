namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class BelongsToAttribute : Attribute
{
    public Type RelatedModelType { get; }
    public string LocalForeignKeyPropertyName { get; }

    public BelongsToAttribute(Type relatedModelType, string localForeignKeyPropertyName)
    {
        RelatedModelType = relatedModelType;
        LocalForeignKeyPropertyName = localForeignKeyPropertyName;
    }
}