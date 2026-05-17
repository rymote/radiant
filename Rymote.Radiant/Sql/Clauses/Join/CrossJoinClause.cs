using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Join;

public sealed class CrossJoinClause : IQueryClause
{
    public string TableName { get; }
    public string? SchemaName { get; }
    public string? Alias { get; }

    public CrossJoinClause(string tableName, string? schemaName = null, string? alias = null)
    {
        TableName = tableName;
        SchemaName = schemaName;
        Alias = alias;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteRaw("CROSS JOIN").WriteSpace();

        emitter.WriteQualifiedName(SchemaName, TableName);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }
}
