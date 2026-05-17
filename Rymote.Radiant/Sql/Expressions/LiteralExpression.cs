using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class LiteralExpression : ISqlExpression
{
    public object Value { get; }

    public LiteralExpression(object value)
    {
        Value = value;
    }

    public void Accept(SqlEmitter emitter)
    {
        if (Value is null)
            emitter.WriteKeyword(emitter.Dialect.NullLiteral);
        else
            emitter.WritePlaceholderForValue(Value);
    }

    public static LiteralExpression String(string value) => new(value);
    public static LiteralExpression Number(int value) => new(value);
    public static LiteralExpression Number(decimal value) => new(value);
    public static LiteralExpression Boolean(bool value) => new(value);
    public static LiteralExpression Null() => new(null!);
}
