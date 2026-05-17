using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereGroup : IWhereExpression
{
    private readonly List<(IWhereExpression Expression, WhereLogicalOperator? Operator)> expressions = new();

    internal WhereGroup() { }

    /// <summary>
    /// Read-only view of the expressions held by this group along with their preceding logical
    /// operator (null for the first expression). Exposed so callers like SmartQuery's bulk
    /// UPDATE / DELETE can graft a WhereClause from one builder onto another.
    /// </summary>
    public IReadOnlyList<(IWhereExpression Expression, WhereLogicalOperator? Operator)> Expressions => expressions;

    /// <summary>
    /// Adds an already-constructed <see cref="IWhereExpression"/> with the given logical operator.
    /// The first expression added always has a null preceding operator, regardless of what is passed.
    /// </summary>
    public WhereGroup AddExpression(IWhereExpression expression, WhereLogicalOperator? logicalOperator)
    {
        expressions.Add((expression, expressions.Count == 0 ? null : logicalOperator));
        return this;
    }

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

    public bool HasConditions => expressions.Count > 0;

    public void Accept(SqlEmitter emitter)
    {
        if (expressions.Count == 0)
            return;

        bool needsParentheses = expressions.Count > 1 && expressions.Any(valueTuple => valueTuple.Operator == WhereLogicalOperator.Or);

        if (needsParentheses)
            emitter.WriteRaw("(");

        foreach ((IWhereExpression expression, WhereLogicalOperator? logicalOperator) in expressions)
        {
            if (logicalOperator.HasValue)
                emitter.WriteSpace()
                    .WriteRaw(logicalOperator == WhereLogicalOperator.And ? "AND" : "OR")
                    .WriteSpace();

            emitter.Emit(expression);
        }

        if (needsParentheses)
            emitter.WriteRaw(")");
    }
}
