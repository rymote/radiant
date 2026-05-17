using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Delete;

public sealed class DeleteClause : IQueryClause
{
    public void AppendTo(StringBuilder stringBuilder, ParameterBag _)
    {
        stringBuilder.Append(SqlKeywords.DELETE).Append(SqlKeywords.SPACE);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.Delete).WriteSpace();
    }
}