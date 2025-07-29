using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereClause : IQueryClause
{
    private readonly WhereGroup rootGroup = new();
    
    public WhereClause Where(string columnName, string operatorSymbol, object value)
    {
        rootGroup.And(columnName, operatorSymbol, value);
        return this;
    }

    public WhereClause And(string columnName, string operatorSymbol, object value)
    {
        rootGroup.And(columnName, operatorSymbol, value);
        return this;
    }

    public WhereClause Or(string columnName, string operatorSymbol, object value)
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

    public WhereClause AddCondition(string columnName, string operatorSymbol, object value)
    {
        return Where(columnName, operatorSymbol, value);
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (!rootGroup.HasConditions) return;

        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.WHERE)
            .Append(SqlKeywords.SPACE);
        
        rootGroup.AppendTo(stringBuilder, parameterBag);
    }

    public bool HasConditions => rootGroup.HasConditions;
}