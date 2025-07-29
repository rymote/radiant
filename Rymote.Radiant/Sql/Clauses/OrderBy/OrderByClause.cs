using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.ORDER_BY)
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.QUOTE)
            .Append(ColumnName)
            .Append(SqlKeywords.QUOTE)
            .Append(SqlKeywords.SPACE)
            .Append(Direction == SortDirection.Descending ? 
                SqlKeywords.DESC : 
                SqlKeywords.ASC);
    }
}
