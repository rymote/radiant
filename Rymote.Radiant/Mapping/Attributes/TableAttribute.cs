namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    public string TableName { get; }
    public TableAttribute(string tableName) => TableName = tableName;
}