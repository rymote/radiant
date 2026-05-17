using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        if (!string.IsNullOrEmpty(TableAlias))
            emitter.WriteIdentifier(TableAlias).WriteRaw(".").WriteIdentifier(ColumnName);
        else
            emitter.WriteIdentifier(ColumnName);
    }
}
