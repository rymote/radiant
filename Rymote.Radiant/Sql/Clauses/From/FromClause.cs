using System.Text;
using Rymote.Radiant.Sql.Clauses.Table;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.From;

public sealed class FromClause : TableClause
{
    public FromClause(string tableName,
        string? schemaName = null,
        string? alias = null)
        : base(tableName, schemaName, alias)
    {
    }

    public override void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.FROM).Append(SqlKeywords.SPACE);
        base.AppendTo(stringBuilder, parameterBag);
    }

    public override void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.From).WriteSpace();
        base.Accept(emitter);
    }
}