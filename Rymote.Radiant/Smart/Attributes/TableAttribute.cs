namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TableAttribute : Attribute
{
    public string TableName { get; }
    public string? SchemaName { get; }

    public TableAttribute(string tableName, string? schemaName = null)
    {
        TableName = tableName;
        SchemaName = schemaName;
    }
}