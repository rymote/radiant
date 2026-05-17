using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses;

public interface IQueryClause
{
    /// <summary>Adapter-aware emission; the canonical path.</summary>
    void Accept(SqlEmitter emitter);
}
