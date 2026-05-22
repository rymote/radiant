using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereClause : IQueryClause
{
    private readonly WhereGroup rootGroup = new();

    /// <summary>
    /// The top-level <see cref="WhereGroup"/>. Exposed to callers that need to graft conditions
    /// from one clause onto another (for example, SmartQuery's bulk UPDATE / DELETE path
    /// transfers a SELECT's WHERE into the UPDATE/DELETE builder).
    /// </summary>
    public WhereGroup RootGroup => rootGroup;

    public WhereClause Where(string columnName, string operatorSymbol, object? value)
    {
        rootGroup.And(columnName, operatorSymbol, value);
        return this;
    }

    public WhereClause And(string columnName, string operatorSymbol, object? value)
    {
        rootGroup.And(columnName, operatorSymbol, value);
        return this;
    }

    public WhereClause Or(string columnName, string operatorSymbol, object? value)
    {
        rootGroup.Or(columnName, operatorSymbol, value);
        return this;
    }

    public WhereClause And(Action<WhereGroup> groupBuilder)
    {
        rootGroup.And(groupBuilder);
        return this;
    }

    public WhereClause Or(Action<WhereGroup> groupBuilder)
    {
        rootGroup.Or(groupBuilder);
        return this;
    }

    public WhereClause AddCondition(string columnName, string operatorSymbol, object? value)
    {
        return Where(columnName, operatorSymbol, value);
    }

    public WhereClause AddBooleanExpression(ISqlExpression booleanExpression)
    {
        rootGroup.AndBooleanExpression(booleanExpression);
        return this;
    }

    public bool HasConditions => rootGroup.HasConditions;

    public void Accept(SqlEmitter emitter)
    {
        if (!rootGroup.HasConditions) return;

        emitter.WriteSpace().WriteKeyword(emitter.Dialect.Where).WriteSpace();
        emitter.Emit(rootGroup);
    }
}
