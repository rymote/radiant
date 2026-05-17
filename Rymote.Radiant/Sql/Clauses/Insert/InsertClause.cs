using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class InsertClause : IQueryClause
{
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.INSERT_INTO).Append(SqlKeywords.SPACE);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.InsertInto).WriteSpace();
    }
}