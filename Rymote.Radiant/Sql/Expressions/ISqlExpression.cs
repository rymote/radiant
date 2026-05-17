using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public interface ISqlExpression
{
    /// <summary>Adapter-aware emission; the canonical path.</summary>
    void Accept(SqlEmitter emitter);
}
