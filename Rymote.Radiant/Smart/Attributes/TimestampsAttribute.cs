namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TimestampsAttribute : Attribute
{
    public string CreatedAtPropertyName { get; }
    public string UpdatedAtPropertyName { get; }

    public TimestampsAttribute(string createdAtPropertyName = "CreatedAt", string updatedAtPropertyName = "UpdatedAt")
    {
        CreatedAtPropertyName = createdAtPropertyName;
        UpdatedAtPropertyName = updatedAtPropertyName;
    }
}