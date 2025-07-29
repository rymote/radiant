using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Limit;

public sealed class LimitClause : IQueryClause
{
    public int Limit { get; }
    public int? Offset { get; }

    public LimitClause(int limit, int? offset = null)
    {
        Limit = limit;
        Offset = offset;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.LIMIT)
            .Append(SqlKeywords.SPACE)
            .Append(Limit);
        
        if (Offset.HasValue) 
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.OFFSET)
                .Append(SqlKeywords.SPACE)
                .Append(Offset.Value);
    }
}
