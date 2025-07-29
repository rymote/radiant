using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Delete;

public sealed class DeleteClause : IQueryClause
{
    public void AppendTo(StringBuilder stringBuilder, ParameterBag _)
    {
        stringBuilder.Append(SqlKeywords.DELETE).Append(SqlKeywords.SPACE);
    }
}