namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class HasOneAttribute : Attribute
{
    public string ForeignKey { get; }
    public HasOneAttribute(string foreignKey) => ForeignKey = foreignKey;
}