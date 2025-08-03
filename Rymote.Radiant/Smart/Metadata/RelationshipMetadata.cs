using System.Reflection;

namespace Rymote.Radiant.Smart.Metadata;

public sealed class RelationshipMetadata : IRelationshipMetadata
{
    public PropertyInfo PropertyInfo { get; }
    public RelationshipType RelationshipType { get; }
    public Type RelatedModelType { get; }
    public string ForeignKeyPropertyName { get; }

    public RelationshipMetadata(
        PropertyInfo propertyInfo,
        RelationshipType relationshipType,
        Type relatedModelType,
        string foreignKeyPropertyName)
    {
        PropertyInfo = propertyInfo;
        RelationshipType = relationshipType;
        RelatedModelType = relatedModelType;
        ForeignKeyPropertyName = foreignKeyPropertyName;
    }
}