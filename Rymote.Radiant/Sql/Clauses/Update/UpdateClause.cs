using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class UpdateClause : IQueryClause
{

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.UPDATE).Append(SqlKeywords.SPACE);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.Update).WriteSpace();
    }
}