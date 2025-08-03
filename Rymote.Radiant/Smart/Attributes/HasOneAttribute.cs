namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HasOneAttribute : Attribute
{
    public Type RelatedModelType { get; }
    public string ForeignKeyPropertyName { get; }

    public HasOneAttribute(Type relatedModelType, string foreignKeyPropertyName)
    {
        RelatedModelType = relatedModelType;
        ForeignKeyPropertyName = foreignKeyPropertyName;
    }
}