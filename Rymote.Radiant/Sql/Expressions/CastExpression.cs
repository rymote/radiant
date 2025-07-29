using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class CastExpression : ISqlExpression
{
    public ISqlExpression Expression { get; }
    public string TargetType { get; }
    public string? Alias { get; private set; }

    public CastExpression(ISqlExpression expression, string targetType)
    {
        Expression = expression;
        TargetType = targetType;
    }

    public CastExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.OPEN_PAREN);
        Expression.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN).Append(SqlKeywords.CAST_OPERATOR).Append(TargetType);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }
}