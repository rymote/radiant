using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.OrderBy;

public sealed class OrderByClause : IQueryClause
{
    public string ColumnName { get; }
    public SortDirection Direction { get; }

    public OrderByClause(string columnName, SortDirection direction)
    {
        ColumnName = columnName;
        Direction = direction;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace()
            .WriteKeyword(emitter.Dialect.OrderBy)
            .WriteSpace()
            .WriteIdentifier(ColumnName)
            .WriteSpace()
            .WriteKeyword(Direction == SortDirection.Descending
                ? emitter.Dialect.Descending
                : emitter.Dialect.Ascending);
    }
}
