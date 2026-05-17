using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Delete;

public sealed class DeleteClause : IQueryClause
{
    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.Delete).WriteSpace();
    }
}
