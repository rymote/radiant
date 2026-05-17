using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace()
            .WriteKeyword(emitter.Dialect.Limit)
            .WriteSpace()
            .WriteRaw(Limit.ToString());

        if (Offset.HasValue)
            emitter.WriteSpace()
                .WriteKeyword(emitter.Dialect.Offset)
                .WriteSpace()
                .WriteRaw(Offset.Value.ToString());
    }
}
