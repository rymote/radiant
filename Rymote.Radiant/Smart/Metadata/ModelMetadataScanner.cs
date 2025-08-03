using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rymote.Radiant.Smart.Attributes;

namespace Rymote.Radiant.Smart.Metadata;

public sealed class ModelMetadataScanner : IModelMetadataScanner
{
    public IModelMetadata ScanModel(Type modelType)
    {
        TableAttribute? tableAttribute = modelType.GetCustomAttribute<TableAttribute>();
        if (tableAttribute == null)
        {
            throw new InvalidOperationException($"Model type {modelType.Name} must have a TableAttribute");
        }

        Dictionary<string, IPropertyMetadata> properties = new Dictionary<string, IPropertyMetadata>();
        List<IRelationshipMetadata> relationships = new List<IRelationshipMetadata>();

        PropertyInfo[] modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (PropertyInfo propertyInfo in modelProperties)
        {
            if (IsRelationshipProperty(propertyInfo, out IRelationshipMetadata? relationshipMetadata))
            {
                if (relationshipMetadata != null)
                    relationships.Add(relationshipMetadata);
            }
            else
            {
                IPropertyMetadata propertyMetadata = ScanProperty(propertyInfo);
                properties.Add(propertyMetadata.PropertyName, propertyMetadata);
            }
        }

        TimestampsAttribute? timestampsAttribute = modelType.GetCustomAttribute<TimestampsAttribute>();
        SoftDeleteAttribute? softDeleteAttribute = modelType.GetCustomAttribute<SoftDeleteAttribute>();

        return new ModelMetadata(
            modelType,
            tableAttribute.TableName,
            tableAttribute.SchemaName,
            properties,
            relationships,
            timestampsAttribute != null,
            softDeleteAttribute != null,
            timestampsAttribute?.CreatedAtPropertyName,
            timestampsAttribute?.UpdatedAtPropertyName,
            softDeleteAttribute?.DeletedAtPropertyName);
    }

    private IPropertyMetadata ScanProperty(PropertyInfo propertyInfo)
    {
        ColumnAttribute? columnAttribute = propertyInfo.GetCustomAttribute<ColumnAttribute>();
        PrimaryKeyAttribute? primaryKeyAttribute = propertyInfo.GetCustomAttribute<PrimaryKeyAttribute>();
        ForeignKeyAttribute? foreignKeyAttribute = propertyInfo.GetCustomAttribute<ForeignKeyAttribute>();
        IndexAttribute[] indexAttributes = propertyInfo.GetCustomAttributes<IndexAttribute>().ToArray();

        string columnName = columnAttribute?.ColumnName ?? ConvertPropertyNameToColumnName(propertyInfo.Name);
        Type propertyType = columnAttribute?.DataType ?? propertyInfo.PropertyType;
        bool isNullable = columnAttribute?.IsNullable ?? IsNullableType(propertyInfo.PropertyType);

        List<IIndexMetadata> indexes = indexAttributes
            .Select(indexAttribute => new IndexMetadata(
                indexAttribute.IndexName,
                indexAttribute.IsUnique,
                indexAttribute.IsClustered))
            .Cast<IIndexMetadata>()
            .ToList();

        return new PropertyMetadata(
            propertyInfo,
            propertyInfo.Name,
            columnName,
            propertyType,
            primaryKeyAttribute != null,
            primaryKeyAttribute?.IsAutoIncrement ?? false,
            isNullable,
            foreignKeyAttribute != null,
            indexes,
            columnAttribute?.DatabaseType);
    }

    private bool IsRelationshipProperty(PropertyInfo propertyInfo, out IRelationshipMetadata? relationshipMetadata)
    {
        HasOneAttribute? hasOneAttribute = propertyInfo.GetCustomAttribute<HasOneAttribute>();
        if (hasOneAttribute != null)
        {
            relationshipMetadata = new RelationshipMetadata(
                propertyInfo,
                RelationshipType.HasOne,
                hasOneAttribute.RelatedModelType,
                hasOneAttribute.ForeignKeyPropertyName);
            return true;
        }

        HasManyAttribute? hasManyAttribute = propertyInfo.GetCustomAttribute<HasManyAttribute>();
        if (hasManyAttribute != null)
        {
            relationshipMetadata = new RelationshipMetadata(
                propertyInfo,
                RelationshipType.HasMany,
                hasManyAttribute.RelatedModelType,
                hasManyAttribute.ForeignKeyPropertyName);
            return true;
        }

        BelongsToAttribute? belongsToAttribute = propertyInfo.GetCustomAttribute<BelongsToAttribute>();
        if (belongsToAttribute != null)
        {
            relationshipMetadata = new RelationshipMetadata(
                propertyInfo,
                RelationshipType.BelongsTo,
                belongsToAttribute.RelatedModelType,
                belongsToAttribute.LocalForeignKeyPropertyName);
            return true;
        }

        relationshipMetadata = null;
        return false;
    }

    private string ConvertPropertyNameToColumnName(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) => 
            index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }

    private bool IsNullableType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}