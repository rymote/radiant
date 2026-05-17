using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        switch (Function)
        {
            case DateTimeFunction.CurrentDate:
                emitter.WriteRaw("CURRENT_DATE");
                break;

            case DateTimeFunction.CurrentTime:
                emitter.WriteRaw("CURRENT_TIME");
                break;

            case DateTimeFunction.CurrentTimestamp:
                emitter.WriteKeyword(emitter.Dialect.CurrentTimestampExpression);
                break;

            case DateTimeFunction.Now:
                emitter.WriteRaw("NOW()");
                break;

            case DateTimeFunction.DateTrunc:
                emitter.WriteRaw("date_trunc").WriteRaw("(")
                    .WritePlaceholderForValue(Unit)
                    .WriteRaw(", ");
                emitter.Emit(Expression!);
                emitter.WriteRaw(")");
                break;

            case DateTimeFunction.Extract:
                emitter.WriteRaw("EXTRACT").WriteRaw("(")
                    .WriteRaw(Unit ?? string.Empty).WriteSpace()
                    .WriteKeyword(emitter.Dialect.From).WriteSpace();
                emitter.Emit(Expression!);
                emitter.WriteRaw(")");
                break;
        }

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }
}
