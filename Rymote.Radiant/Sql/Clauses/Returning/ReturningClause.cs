using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace()
            .WriteKeyword(emitter.Dialect.ReturningKeyword)
            .WriteSpace();

        for (int index = 0; index < ColumnNames.Count; index++)
        {
            emitter.WriteIdentifier(ColumnNames[index]);

            if (index < ColumnNames.Count - 1)
                emitter.WriteRaw(", ");
        }
    }
}
