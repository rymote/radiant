using System;

namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class AuditAttribute : Attribute
{
    public string CreatedByPropertyName { get; }
    public string UpdatedByPropertyName { get; }

    public AuditAttribute(
        string createdByPropertyName = "CreatedByUserId",
        string updatedByPropertyName = "UpdatedByUserId")
    {
        CreatedByPropertyName = createdByPropertyName;
        UpdatedByPropertyName = updatedByPropertyName;
    }
}
