using System.Text;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class RawSqlExpression : ISqlExpression
{
    public string RawSql { get; }

    public RawSqlExpression(string rawSql)
    {
        RawSql = rawSql;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(RawSql);
    }
}