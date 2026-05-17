using System.Text;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public interface ISqlExpression
{
    /// <summary>Adapter-aware emission; the canonical path.</summary>
    void Accept(SqlEmitter emitter);

    /// <summary>Legacy emission path. Will be removed once every expression routes through <see cref="Accept"/>.</summary>
    void AppendTo(StringBuilder stringBuilder);
}
