using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class SubqueryExpression : ISqlExpression
{
    public IQueryBuilder Query { get; }
    public string? Alias { get; }

    public SubqueryExpression(IQueryBuilder query, string? alias = null)
    {
        Query = query;
        Alias = alias;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.OPEN_PAREN).Append(Query.Build().SqlText).Append(SqlKeywords.CLOSE_PAREN);
        
        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        QueryCommand queryCommand = Query.Build();
        stringBuilder.Append(SqlKeywords.OPEN_PAREN).Append(queryCommand.SqlText).Append(SqlKeywords.CLOSE_PAREN);
        
        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);

        foreach (string parameterName in queryCommand.Parameters.ParameterNames)
        {
            object? value = queryCommand.Parameters.Get<object>(parameterName);
            parameterBag.Add(value);
        }
    }
}