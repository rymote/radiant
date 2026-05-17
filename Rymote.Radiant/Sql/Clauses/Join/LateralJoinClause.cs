using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.SPACE);
        
        if (JoinType != JoinType.Cross)
        {
            stringBuilder.Append(JoinType switch
            {
                JoinType.Inner => SqlKeywords.INNER_JOIN,
                JoinType.Left => SqlKeywords.LEFT_JOIN,
                JoinType.Right => SqlKeywords.RIGHT_JOIN,
                JoinType.Full => SqlKeywords.FULL_JOIN,
                _ => SqlKeywords.INNER_JOIN
            });
        }
        else
        {
            stringBuilder.Append(SqlKeywords.CROSS_JOIN);
        }
        
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.LATERAL).Append(SqlKeywords.SPACE);
        
        QueryCommand subQueryCommand = SubQuery.Build();
        stringBuilder.Append(SqlKeywords.OPEN_PAREN)
            .Append(subQueryCommand)
            .Append(SqlKeywords.CLOSE_PAREN);
            
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);

        foreach (string parameterName in subQueryCommand.Parameters.ParameterNames)
        {
            object? value = subQueryCommand.Parameters.Get<object>(parameterName);
            parameterBag.Add(value);
        }
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

        QueryCommand subQueryCommand = SubQuery.Build();
        emitter.WriteRaw("(").WriteRaw(subQueryCommand.SqlText).WriteRaw(")");
        emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);

        foreach (string parameterName in subQueryCommand.Parameters.ParameterNames)
        {
            object? value = subQueryCommand.Parameters.Get<object>(parameterName);
            emitter.Parameters.Add(value);
        }
    }
}