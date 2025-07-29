using System.Text;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses;

public interface IQueryClause
{
    void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag);
}