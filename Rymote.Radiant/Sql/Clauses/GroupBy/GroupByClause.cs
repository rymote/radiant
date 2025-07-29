using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.GroupBy;

public sealed class GroupByClause : IQueryClause
{
    private readonly List<string> groupByColumns = [];

    public GroupByClause AddColumn(string columnName)
    {
        if (!groupByColumns.Contains(columnName))
            groupByColumns.Add(columnName);
        
        return this;
    }

    public GroupByClause AddColumns(params string[] columnNames)
    {
        foreach (string columnName in columnNames)
            AddColumn(columnName);
            
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (groupByColumns.Count == 0) return;

        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.GROUP_BY)
            .Append(SqlKeywords.SPACE);
        
        for (int index = 0; index < groupByColumns.Count; index++)
        {
            if (index > 0) stringBuilder.Append(SqlKeywords.COMMA);
            stringBuilder.Append(SqlKeywords.QUOTE).Append(groupByColumns[index]).Append(SqlKeywords.QUOTE);
        }
    }

    public bool HasColumns => groupByColumns.Count > 0;
}