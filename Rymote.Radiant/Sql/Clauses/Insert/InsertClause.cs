using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class InsertClause : IQueryClause
{
    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.InsertInto).WriteSpace();
    }
}
