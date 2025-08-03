using System.Reflection;

namespace Rymote.Radiant.Smart.Metadata;

public sealed class PropertyMetadata : IPropertyMetadata
{
    public PropertyInfo PropertyInfo { get; }
    public string PropertyName { get; }
    public string ColumnName { get; }
    public Type PropertyType { get; }
    public bool IsPrimaryKey { get; }
    public bool IsAutoIncrement { get; }
    public bool IsNullable { get; }
    public bool IsForeignKey { get; }
    public IReadOnlyList<IIndexMetadata> Indexes { get; }
    public string? DatabaseType { get; }

    public PropertyMetadata(
        PropertyInfo propertyInfo,
        string propertyName,
        string columnName,
        Type propertyType,
        bool isPrimaryKey,
        bool isAutoIncrement,
        bool isNullable,
        bool isForeignKey,
        IReadOnlyList<IIndexMetadata> indexes,
        string? databaseType = null)
    {
        PropertyInfo = propertyInfo;
        PropertyName = propertyName;
        ColumnName = columnName;
        PropertyType = propertyType;
        IsPrimaryKey = isPrimaryKey;
        IsAutoIncrement = isAutoIncrement;
        IsNullable = isNullable;
        IsForeignKey = isForeignKey;
        Indexes = indexes;
        DatabaseType = databaseType;
    }
}