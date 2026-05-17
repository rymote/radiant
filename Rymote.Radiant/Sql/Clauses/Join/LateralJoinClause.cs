using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Join;

public sealed class LateralJoinClause : IQueryClause
{
    public JoinType JoinType { get; }
    public IQueryBuilder SubQuery { get; }
    public string Alias { get; }

    public LateralJoinClause(JoinType joinType, IQueryBuilder subQuery, string alias)
    {
        JoinType = joinType;
        SubQuery = subQuery;
        Alias = alias;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace();

        if (JoinType != JoinType.Cross)
        {
            emitter.WriteRaw(JoinType switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Full => "FULL JOIN",
                _ => "INNER JOIN"
            });
        }
        else
        {
            emitter.WriteRaw("CROSS JOIN");
        }

        emitter.WriteSpace().WriteKeyword(emitter.Dialect.LateralKeyword).WriteSpace();

        QueryCommand subQueryCommand = SubQuery.Build(emitter.Adapter);
        emitter.WriteRaw("(").WriteRaw(subQueryCommand.SqlText).WriteRaw(")");
        emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);

        foreach (string parameterName in subQueryCommand.Parameters.ParameterNames)
        {
            object? value = subQueryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}
