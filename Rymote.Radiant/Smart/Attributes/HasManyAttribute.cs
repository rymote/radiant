namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HasManyAttribute : Attribute
{
    public Type RelatedModelType { get; }
    public string ForeignKeyPropertyName { get; }

    public HasManyAttribute(Type relatedModelType, string foreignKeyPropertyName)
    {
        RelatedModelType = relatedModelType;
        ForeignKeyPropertyName = foreignKeyPropertyName;
    }
}