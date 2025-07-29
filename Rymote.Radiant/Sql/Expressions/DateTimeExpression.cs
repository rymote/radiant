using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum DateTimeFunction
{
    CurrentDate,
    CurrentTime,
    CurrentTimestamp,
    Now,
    DateTrunc,
    Extract
}

public sealed class DateTimeExpression : ISqlExpression
{
    public DateTimeFunction Function { get; }
    public ISqlExpression? Expression { get; }
    public string? Unit { get; }
    public string? Alias { get; private set; }

    private DateTimeExpression(DateTimeFunction function, ISqlExpression? expression = null, string? unit = null)
    {
        Function = function;
        Expression = expression;
        Unit = unit;
    }

    public static DateTimeExpression CurrentDate() => new(DateTimeFunction.CurrentDate);
    public static DateTimeExpression CurrentTime() => new(DateTimeFunction.CurrentTime);
    public static DateTimeExpression CurrentTimestamp() => new(DateTimeFunction.CurrentTimestamp);
    public static DateTimeExpression Now() => new(DateTimeFunction.Now);

    public static DateTimeExpression DateTrunc(string unit, ISqlExpression expression) => 
        new(DateTimeFunction.DateTrunc, expression, unit);

    public static DateTimeExpression Extract(string unit, ISqlExpression expression) => 
        new(DateTimeFunction.Extract, expression, unit);

    public DateTimeExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        switch (Function)
        {
            case DateTimeFunction.CurrentDate:
                stringBuilder.Append(SqlKeywords.CURRENT_DATE);
                break;
            
            case DateTimeFunction.CurrentTime:
                stringBuilder.Append(SqlKeywords.CURRENT_TIME);
                break;
            
            case DateTimeFunction.CurrentTimestamp:
                stringBuilder.Append(SqlKeywords.CURRENT_TIMESTAMP);
                break;
            
            case DateTimeFunction.Now:
                stringBuilder.Append(SqlKeywords.NOW_FUNCTION);
                break;
            
            case DateTimeFunction.DateTrunc:
                stringBuilder.Append(SqlKeywords.DATE_TRUNC).Append(SqlKeywords.OPEN_PAREN)
                    .Append(SqlKeywords.SINGLE_QUOTE).Append(Unit).Append(SqlKeywords.SINGLE_QUOTE)
                    .Append(SqlKeywords.COMMA);
                Expression!.AppendTo(stringBuilder);
                stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
                break;
            
            case DateTimeFunction.Extract:
                stringBuilder.Append(SqlKeywords.EXTRACT).Append(SqlKeywords.OPEN_PAREN)
                    .Append(Unit).Append(SqlKeywords.SPACE).Append(SqlKeywords.FROM).Append(SqlKeywords.SPACE);
                Expression!.AppendTo(stringBuilder);
                stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
                break;
        }

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }
}