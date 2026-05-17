using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Having;

public sealed class HavingClause : IQueryClause
{
    private readonly WhereGroup havingGroup = new();

    public HavingClause Having(string expression, string operatorSymbol, object value)
    {
        havingGroup.And(expression, operatorSymbol, value);
        return this;
    }

    public HavingClause And(string expression, string operatorSymbol, object value)
    {
        havingGroup.And(expression, operatorSymbol, value);
        return this;
    }

    public HavingClause Or(string expression, string operatorSymbol, object value)
    {
        havingGroup.Or(expression, operatorSymbol, value);
        return this;
    }

    public HavingClause And(Action<WhereGroup> groupBuilder)
    {
        havingGroup.And(groupBuilder);
        return this;
    }

    public HavingClause Or(Action<WhereGroup> groupBuilder)
    {
        havingGroup.Or(groupBuilder);
        return this;
    }

    public bool HasConditions => havingGroup.HasConditions;

    public void Accept(SqlEmitter emitter)
    {
        if (!havingGroup.HasConditions) return;

        emitter.WriteSpace().WriteKeyword(emitter.Dialect.Having).WriteSpace();
        emitter.Emit(havingGroup);
    }
}
