namespace Rymote.Radiant.Smart.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ColumnAttribute : Attribute
{
    public string ColumnName { get; }
    public Type? DataType { get; }
    public bool IsNullable { get; }
    public string? DatabaseType { get; }

    public ColumnAttribute(string columnName, Type? dataType = null, bool isNullable = true, string? databaseType = null)
    {
        ColumnName = columnName;
        DataType = dataType;
        IsNullable = isNullable;
        DatabaseType = databaseType;
    }
}