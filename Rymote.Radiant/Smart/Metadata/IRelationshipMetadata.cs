using System.Reflection;

namespace Rymote.Radiant.Smart.Metadata;

public interface IRelationshipMetadata
{
    PropertyInfo PropertyInfo { get; }
    RelationshipType RelationshipType { get; }
    Type RelatedModelType { get; }
    string ForeignKeyPropertyName { get; }
}