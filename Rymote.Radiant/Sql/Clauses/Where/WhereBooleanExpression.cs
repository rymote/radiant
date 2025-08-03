using System.Text;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereBooleanExpression : IWhereExpression
{
    public ISqlExpression Expression { get; }

    public WhereBooleanExpression(ISqlExpression expression)
    {
        Expression = expression;
    }
    
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        Expression.AppendTo(stringBuilder);
    }
}