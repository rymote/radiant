using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw("(");
        emitter.Emit(Expression);
        emitter.WriteRaw(")").WriteKeyword(emitter.Dialect.CastOperator).WriteRaw(TargetType);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }
}
