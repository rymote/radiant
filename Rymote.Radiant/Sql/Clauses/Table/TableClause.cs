using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public virtual void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
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
    }
}