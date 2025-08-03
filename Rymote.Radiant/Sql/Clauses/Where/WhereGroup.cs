using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;
using System.Linq;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereGroup : IWhereExpression
{
    private readonly List<(IWhereExpression Expression, WhereLogicalOperator? Operator)> expressions = new();

    internal WhereGroup() { }

    public WhereGroup And(string columnName, string operatorSymbol, object value)
    {
        WhereCondition condition = new WhereCondition(columnName, operatorSymbol, value);
        expressions.Add((condition, expressions.Count == 0 ? null : WhereLogicalOperator.And));
        return this;
    }

    public WhereGroup Or(string columnName, string operatorSymbol, object value)
    {
        WhereCondition condition = new WhereCondition(columnName, operatorSymbol, value);
        expressions.Add((condition, expressions.Count == 0 ? null : WhereLogicalOperator.Or));
        return this;
    }

    public WhereGroup And(Action<WhereGroup> groupBuilder)
    {
        WhereGroup group = new WhereGroup();
        groupBuilder(group);
        
        if (group.expressions.Count > 0)
            expressions.Add((group, expressions.Count == 0 ? null : WhereLogicalOperator.And));
            
        return this;
    }

    public WhereGroup Or(Action<WhereGroup> groupBuilder)
    {
        WhereGroup group = new WhereGroup();
        groupBuilder(group);
        
        if (group.expressions.Count > 0)
            expressions.Add((group, expressions.Count == 0 ? null : WhereLogicalOperator.Or));
        
        return this;
    }

    public void AndBooleanExpression(ISqlExpression booleanExpression)
    {
        WhereBooleanExpression whereBooleanExpression = new WhereBooleanExpression(booleanExpression);
        expressions.Add((whereBooleanExpression, expressions.Count == 0 ? null : WhereLogicalOperator.And));
    }
    
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (expressions.Count == 0) 
            return;

        bool needsParentheses = expressions.Count > 1 && expressions.Any(valueTuple => valueTuple.Operator == WhereLogicalOperator.Or);
        
        if (needsParentheses) 
            stringBuilder.Append(SqlKeywords.OPEN_PAREN);

        foreach ((IWhereExpression expression, WhereLogicalOperator? logicalOperator) in expressions)
        {
            if (logicalOperator.HasValue)
                stringBuilder
                    .Append(SqlKeywords.SPACE)
                    .Append(logicalOperator == WhereLogicalOperator.And ? SqlKeywords.AND : SqlKeywords.OR)
                    .Append(SqlKeywords.SPACE);

            expression.AppendTo(stringBuilder, parameterBag);
        }

        if (needsParentheses) 
            stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
    }

    public bool HasConditions => expressions.Count > 0;
}