using System.Text;
using Rymote.Radiant.Sql.Clauses;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Returning;

public sealed class ReturningClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }

    public ReturningClause(IEnumerable<string> columnNames)
    {
        ArgumentNullException.ThrowIfNull(columnNames);

        List<string> names = columnNames.Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (names.Count == 0)
            throw new ArgumentException("At least one column name must be provided.", nameof(columnNames));

        ColumnNames = names;
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.RETURNING)
            .Append(SqlKeywords.SPACE);

        for (int index = 0; index < ColumnNames.Count; index++)
        {
            stringBuilder.Append(SqlKeywords.QUOTE).Append(ColumnNames[index]).Append(SqlKeywords.QUOTE);

            if (index < ColumnNames.Count - 1)
                stringBuilder.Append(SqlKeywords.COMMA);
        }
    }
}