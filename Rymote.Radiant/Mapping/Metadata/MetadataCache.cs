using System.Collections.Concurrent;
using System.Reflection;
using Dapper;
using Rymote.Radiant.Mapping.Attributes;

namespace Rymote.Radiant.Mapping.Metadata;

public sealed class MetadataCache
{
    private readonly ConcurrentDictionary<Type, ModelMetadata> modelMetadataCache = new();

    public ModelMetadata GetMetadata(Type modelType)
    {
        return modelMetadataCache.GetOrAdd(modelType, BuildMetadata);
    }

    private ModelMetadata BuildMetadata(Type modelType)
    {
        string resolvedTableName = modelType.GetCustomAttribute<TableAttribute>()?.TableName ?? modelType.Name;

        List<ColumnMetadata> columnMetadataList = modelType.GetProperties()
            .Select(BuildColumnMetadata)
            .ToList();

        SqlMapper.SetTypeMap(modelType,
            new CustomPropertyTypeMap(modelType,
                (_, columnName) =>
                    columnMetadataList
                        .FirstOrDefault(columnMetadata => columnMetadata.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        ?.PropertyInfo!));

        return new ModelMetadata(resolvedTableName, modelType, columnMetadataList);
    }

    private static ColumnMetadata BuildColumnMetadata(PropertyInfo propertyInfo)
    {
        string resolvedColumnName = propertyInfo.GetCustomAttribute<ColumnAttribute>()?.ColumnName ?? propertyInfo.Name;
        bool isPrimaryKeyColumn = propertyInfo.GetCustomAttribute<PrimaryKeyAttribute>() != null;
        return new ColumnMetadata(resolvedColumnName, propertyInfo, isPrimaryKeyColumn);
    }
}