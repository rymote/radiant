using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Where;

public interface IWhereExpression
{
    /// <summary>Adapter-aware emission; the canonical path.</summary>
    void Accept(SqlEmitter emitter);
}
