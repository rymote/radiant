using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Filter;

public class FilterClause : IQueryClause
{
    public void Accept(SqlEmitter emitter)
    {
        throw new NotImplementedException("FilterClause emission is not yet implemented.");
    }
}
