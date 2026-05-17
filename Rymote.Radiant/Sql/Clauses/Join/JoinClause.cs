using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Join;

public sealed class JoinClause : IQueryClause
{
    public JoinType JoinType { get; }
    public string TableName { get; }
    public string? SchemaName { get; }
    public string? Alias { get; }
    public string LeftColumn { get; }
    public string RightColumn { get; }

    public JoinClause(JoinType joinType, string tableName, string? schemaName, string? alias, string leftColumn,
        string rightColumn)
    {
        JoinType = joinType;
        TableName = tableName;
        SchemaName = schemaName;
        Alias = alias;
        LeftColumn = leftColumn;
        RightColumn = rightColumn;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteRaw(JoinType switch
        {
            JoinType.Inner => "INNER JOIN",
            JoinType.Left => "LEFT JOIN",
            JoinType.Right => "RIGHT JOIN",
            _ => "FULL JOIN"
        }).WriteSpace();

        emitter.WriteQualifiedName(SchemaName, TableName);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteIdentifier(Alias);

        emitter.WriteSpace().WriteRaw("ON").WriteSpace()
            .WriteIdentifier(LeftColumn)
            .WriteRaw(" = ")
            .WriteIdentifier(RightColumn);
    }
}
