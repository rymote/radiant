using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class UpdateClause : IQueryClause
{
    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.Update).WriteSpace();
    }
}
