using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.Sqlite;

public sealed class SqliteIdentifierQuoter : IIdentifierQuoter
{
    public string QuoteIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    public string QuoteQualifiedName(string? schemaName, string objectName)
        => string.IsNullOrWhiteSpace(schemaName)
            ? QuoteIdentifier(objectName)
            : QuoteIdentifier(schemaName) + "." + QuoteIdentifier(objectName);
}
