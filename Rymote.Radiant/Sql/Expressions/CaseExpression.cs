using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class CaseExpression : ISqlExpression
{
    private readonly List<(string Column, string Operator, object Value, object Result)> whenClauses = new();
    private object? elseResult;
    private string? alias;

    public CaseExpression When(string column, string operatorSymbol, object value, object result)
    {
        whenClauses.Add((column, operatorSymbol, value, result));
        return this;
    }

    public CaseExpression Else(object result)
    {
        elseResult = result;
        return this;
    }

    public CaseExpression As(string aliasName)
    {
        alias = aliasName;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.CASE);
        
        foreach ((string column, string operatorSymbol, object value, object result) in whenClauses)
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.WHEN).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(column).Append(SqlKeywords.QUOTE)
                .Append(SqlKeywords.SPACE).Append(operatorSymbol).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.SINGLE_QUOTE).Append(value).Append(SqlKeywords.SINGLE_QUOTE)
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.THEN).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.SINGLE_QUOTE).Append(result).Append(SqlKeywords.SINGLE_QUOTE);

        if (elseResult != null)
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.ELSE).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.SINGLE_QUOTE).Append(elseResult).Append(SqlKeywords.SINGLE_QUOTE);

        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.END);

        if (!string.IsNullOrEmpty(alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(alias).Append(SqlKeywords.QUOTE);
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.CASE);
        
        foreach ((string column, string operatorSymbol, object value, object result) in whenClauses)
        {
            string valueParam = parameterBag.Add(value);
            string resultParam = parameterBag.Add(result);
            
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.WHEN).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(column).Append(SqlKeywords.QUOTE)
                .Append(SqlKeywords.SPACE).Append(operatorSymbol).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.PARAMETER_PREFIX).Append(valueParam)
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.THEN).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.PARAMETER_PREFIX).Append(resultParam);
        }

        if (elseResult != null)
        {
            string elseParam = parameterBag.Add(elseResult);
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.ELSE).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.PARAMETER_PREFIX).Append(elseParam);
        }

        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.END);

        if (!string.IsNullOrEmpty(alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(alias).Append(SqlKeywords.QUOTE);
    }
}