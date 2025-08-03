namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ForeignKeyAttribute : Attribute
{
    public Type RelatedModelType { get; }
    public string RelatedPropertyName { get; }

    public ForeignKeyAttribute(Type relatedModelType, string relatedPropertyName)
    {
        RelatedModelType = relatedModelType;
        RelatedPropertyName = relatedPropertyName;
    }
}