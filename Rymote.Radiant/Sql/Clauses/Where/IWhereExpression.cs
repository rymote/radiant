using System.Text;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Where;

public interface IWhereExpression
{
    void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag);
}