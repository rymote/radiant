using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereBooleanExpression : IWhereExpression
{
    public ISqlExpression Expression { get; }

    public WhereBooleanExpression(ISqlExpression expression)
    {
        Expression = expression;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(Expression);
    }
}
