using Rymote.Radiant.Sql.Clauses.Filter;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class AggregateExpression : ISqlExpression
{
    public FilterClause? FilterClause { get; set; }
    public string FunctionName { get; }
    public ISqlExpression InnerExpression { get; }
    public bool IsDistinct { get; }

    public AggregateExpression(string functionName, ISqlExpression innerExpression, bool isDistinct = false)
    {
        FunctionName = functionName;
        InnerExpression = innerExpression;
        IsDistinct = isDistinct;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(FunctionName).WriteRaw("(");

        if (IsDistinct)
            emitter.WriteRaw("DISTINCT").WriteSpace();

        emitter.Emit(InnerExpression);
        emitter.WriteRaw(")");
    }
}
