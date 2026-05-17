using Rymote.Radiant.Sql.Clauses.Table;
using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.From;

public sealed class FromClause : TableClause
{
    public FromClause(string tableName,
        string? schemaName = null,
        string? alias = null)
        : base(tableName, schemaName, alias)
    {
    }

    public override void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.From).WriteSpace();
        base.Accept(emitter);
    }
}
