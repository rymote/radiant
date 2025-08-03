namespace Rymote.Radiant.Smart.Metadata;

public sealed class ModelMetadata : IModelMetadata
{
    public Type ModelType { get; }
    public string TableName { get; }
    public string? SchemaName { get; }
    public IReadOnlyDictionary<string, IPropertyMetadata> Properties { get; }
    public IPropertyMetadata? PrimaryKey { get; }
    public IReadOnlyList<IRelationshipMetadata> Relationships { get; }
    public bool HasTimestamps { get; }
    public bool HasSoftDelete { get; }
    public string? CreatedAtPropertyName { get; }
    public string? UpdatedAtPropertyName { get; }
    public string? DeletedAtPropertyName { get; }

    public ModelMetadata(
        Type modelType,
        string tableName,
        string? schemaName,
        IReadOnlyDictionary<string, IPropertyMetadata> properties,
        IReadOnlyList<IRelationshipMetadata> relationships,
        bool hasTimestamps,
        bool hasSoftDelete,
        string? createdAtPropertyName,
        string? updatedAtPropertyName,
        string? deletedAtPropertyName)
    {
        ModelType = modelType;
        TableName = tableName;
        SchemaName = schemaName;
        Properties = properties;
        PrimaryKey = properties.Values.FirstOrDefault(property => property.IsPrimaryKey);
        Relationships = relationships;
        HasTimestamps = hasTimestamps;
        HasSoftDelete = hasSoftDelete;
        CreatedAtPropertyName = createdAtPropertyName;
        UpdatedAtPropertyName = updatedAtPropertyName;
        DeletedAtPropertyName = deletedAtPropertyName;
    }
}