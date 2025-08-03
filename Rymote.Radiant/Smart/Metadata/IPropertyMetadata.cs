using System.Reflection;

namespace Rymote.Radiant.Smart.Metadata;

public interface IPropertyMetadata
{
    PropertyInfo PropertyInfo { get; }
    string PropertyName { get; }
    string ColumnName { get; }
    Type PropertyType { get; }
    bool IsPrimaryKey { get; }
    bool IsAutoIncrement { get; }
    bool IsNullable { get; }
    bool IsForeignKey { get; }
    IReadOnlyList<IIndexMetadata> Indexes { get; }
    string? DatabaseType { get; }
}