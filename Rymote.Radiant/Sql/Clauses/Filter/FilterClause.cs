using System.Text;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Filter;

public class FilterClause : IQueryClause
{
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        throw new NotImplementedException();
    }
}