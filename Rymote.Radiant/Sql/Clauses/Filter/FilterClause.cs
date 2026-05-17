using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Filter;

public class FilterClause : IQueryClause
{
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        throw new NotImplementedException();
    }

    public void Accept(SqlEmitter emitter)
    {
        throw new NotImplementedException("FilterClause emission is not yet implemented.");
    }
}