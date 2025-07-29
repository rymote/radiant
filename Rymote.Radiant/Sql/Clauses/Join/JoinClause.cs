using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(JoinType switch
            {
                JoinType.Inner => SqlKeywords.INNER_JOIN,
                JoinType.Left => SqlKeywords.LEFT_JOIN,
                JoinType.Right => SqlKeywords.RIGHT_JOIN,
                _ => SqlKeywords.FULL_JOIN
            })
            .Append(SqlKeywords.SPACE);

        if (!string.IsNullOrEmpty(SchemaName))
            stringBuilder
                .Append(SqlKeywords.QUOTE)
                .Append(SchemaName)
                .Append(SqlKeywords.QUOTE)
                .Append(SqlKeywords.DOT);

        stringBuilder
            .Append(SqlKeywords.QUOTE)
            .Append(TableName)
            .Append(SqlKeywords.QUOTE);
        
        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE)
                .Append(Alias)
                .Append(SqlKeywords.QUOTE);

        stringBuilder
            .Append(SqlKeywords.SPACE).Append(SqlKeywords.ON).Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.QUOTE).Append(LeftColumn).Append(SqlKeywords.QUOTE)
            .Append(SqlKeywords.EQUALS)
            .Append(SqlKeywords.QUOTE).Append(RightColumn).Append(SqlKeywords.QUOTE);
    }
}