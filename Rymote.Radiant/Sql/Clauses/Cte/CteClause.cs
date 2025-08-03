using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Cte;

public sealed class CteClause : IQueryClause
{
    public string Name { get; }
    public IQueryBuilder Query { get; }
    public IReadOnlyList<string> ColumnNames { get; }
    public bool IsRecursive { get; }

    public CteClause(string name, IQueryBuilder query, IReadOnlyList<string>? columnNames = null,
        bool isRecursive = false)
    {
        Name = name;
        Query = query;
        ColumnNames = columnNames ?? [];
        IsRecursive = isRecursive;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.QUOTE).Append(Name).Append(SqlKeywords.QUOTE);

        if (ColumnNames.Count > 0)
        {
            stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);
            for (int index = 0; index < ColumnNames.Count; index++)
            {
                if (index > 0)
                    stringBuilder.Append(SqlKeywords.COMMA);

                stringBuilder.Append(SqlKeywords.QUOTE).Append(ColumnNames[index]).Append(SqlKeywords.QUOTE);
            }

            stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
        }

        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.AS)
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.CLOSE_PAREN);

        QueryCommand queryCommand = Query.Build();
        stringBuilder.Append(queryCommand);
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            parameterBag.Add(value);
        }
    }
}