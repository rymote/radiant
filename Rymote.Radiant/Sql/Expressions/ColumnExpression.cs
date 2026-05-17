using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class ColumnExpression : ISqlExpression
{
    public string? TableAlias { get; }
    public string ColumnName { get; }

    public ColumnExpression(string columnName, string? tableAlias = null)
    {
        ColumnName = columnName;
        TableAlias = tableAlias;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        if (!string.IsNullOrEmpty(TableAlias))
            stringBuilder.Append(SqlKeywords.QUOTE).Append(TableAlias).Append(SqlKeywords.QUOTE).Append(SqlKeywords.DOT);

        stringBuilder.Append(SqlKeywords.QUOTE).Append(ColumnName).Append(SqlKeywords.QUOTE);
    }

    public void Accept(SqlEmitter emitter)
    {
        if (!string.IsNullOrEmpty(TableAlias))
            emitter.WriteIdentifier(TableAlias).WriteRaw(".").WriteIdentifier(ColumnName);
        else
            emitter.WriteIdentifier(ColumnName);
    }
}