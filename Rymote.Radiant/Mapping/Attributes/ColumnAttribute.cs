namespace Rymote.Radiant.Mapping.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    public string ColumnName { get; }
    public ColumnAttribute(string columnName) => ColumnName = columnName;
}