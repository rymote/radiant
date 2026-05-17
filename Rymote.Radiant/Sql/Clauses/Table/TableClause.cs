using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Table;

public class TableClause : IQueryClause
{
    public string TableName { get; }
    public string? SchemaName { get; }
    public string? Alias { get; }

    public TableClause(string tableName, string? schemaName = null, string? alias = null)
    {
        TableName = tableName;
        SchemaName = schemaName;
        Alias = alias;
    }

    public virtual void Accept(SqlEmitter emitter)
    {
        emitter.WriteQualifiedName(SchemaName, TableName);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteIdentifier(Alias);
    }
}
