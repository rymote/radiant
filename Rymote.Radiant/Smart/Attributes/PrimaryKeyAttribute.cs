namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PrimaryKeyAttribute : Attribute
{
    public bool IsAutoIncrement { get; }

    public PrimaryKeyAttribute(bool isAutoIncrement = true)
    {
        IsAutoIncrement = isAutoIncrement;
    }
}