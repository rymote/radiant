using System.Reflection;

namespace Rymote.Radiant.Mapping.Metadata;

public sealed class ColumnMetadata
{
    public string ColumnName { get; }
    public PropertyInfo? PropertyInfo { get; }
    public bool IsPrimaryKey { get; }

    public ColumnMetadata(string columnName, PropertyInfo? propertyInfo, bool isPrimaryKey)
    {
        ColumnName = columnName;
        PropertyInfo = propertyInfo;
        IsPrimaryKey = isPrimaryKey;
    }
}