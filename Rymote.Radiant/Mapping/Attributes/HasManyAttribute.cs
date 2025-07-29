namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class HasManyAttribute : Attribute
{
    public string ForeignKey { get; }
    public HasManyAttribute(string foreignKey) => ForeignKey = foreignKey;
}