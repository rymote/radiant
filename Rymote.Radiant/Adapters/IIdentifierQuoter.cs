namespace Rymote.Radiant.Adapters;

public interface IIdentifierQuoter
{
    string QuoteIdentifier(string identifier);
    string QuoteQualifiedName(string? schemaName, string objectName);
}
