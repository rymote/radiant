using System.Text;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (!havingGroup.HasConditions) return;

        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.HAVING)
            .Append(SqlKeywords.SPACE);
        
        havingGroup.AppendTo(stringBuilder, parameterBag);
    }

    public bool HasConditions => havingGroup.HasConditions;
}