using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Where;

public interface IWhereExpression
{
    /// <summary>Adapter-aware emission; the canonical path.</summary>
    void Accept(SqlEmitter emitter);

    /// <summary>Legacy emission path. Will be removed once every where-expression routes through <see cref="Accept"/>.</summary>
    void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag);
}
