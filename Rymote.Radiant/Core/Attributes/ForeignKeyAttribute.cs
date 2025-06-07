namespace Rymote.Radiant.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    public string ReferenceTable { get; }
    public string ReferenceColumn { get; set; } = "id";
    public string Schema { get; set; } = "public";

    public ForeignKeyAttribute(string referenceTable)
    {
        ReferenceTable = referenceTable;
    }
}