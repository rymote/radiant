namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SoftDeleteAttribute : Attribute
{
    public string DeletedAtPropertyName { get; }

    public SoftDeleteAttribute(string deletedAtPropertyName = "DeletedAt")
    {
        DeletedAtPropertyName = deletedAtPropertyName;
    }
}