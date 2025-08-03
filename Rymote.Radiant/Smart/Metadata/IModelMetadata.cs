namespace Rymote.Radiant.Smart.Metadata;

public interface IModelMetadata
{
    Type ModelType { get; }
    string TableName { get; }
    string? SchemaName { get; }
    IReadOnlyDictionary<string, IPropertyMetadata> Properties { get; }
    IPropertyMetadata? PrimaryKey { get; }
    IReadOnlyList<IRelationshipMetadata> Relationships { get; }
    bool HasTimestamps { get; }
    bool HasSoftDelete { get; }
    string? CreatedAtPropertyName { get; }
    string? UpdatedAtPropertyName { get; }
    string? DeletedAtPropertyName { get; }
}