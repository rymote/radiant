using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class RawSqlExpression : ISqlExpression
{
    public string RawSql { get; }

    public RawSqlExpression(string rawSql)
    {
        RawSql = rawSql;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(RawSql);
    }
}
