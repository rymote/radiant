using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class SetExpressionAssignment : SetAssignment
{
    public ISqlExpression Expression { get; }

    public SetExpressionAssignment(string columnName, ISqlExpression expression) : base(columnName)
    {
        Expression = expression;
    }

    public override void AcceptValue(SqlEmitter emitter)
    {
        emitter.Emit(Expression);
    }
}
