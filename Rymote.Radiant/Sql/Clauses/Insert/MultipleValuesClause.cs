using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class MultipleValuesClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }
    private readonly List<List<object>> valueRows = [];

    public MultipleValuesClause(IReadOnlyList<string> columnNames)
    {
        ColumnNames = columnNames;
    }

    public void AddValueRow(IReadOnlyList<object> values)
    {
        if (values.Count != ColumnNames.Count)
            throw new ArgumentException($"Value count ({values.Count}) must match column count ({ColumnNames.Count})");

        valueRows.Add(values.ToList());
    }

    public int RowCount => valueRows.Count;

    public void Accept(SqlEmitter emitter)
    {
        if (valueRows.Count == 0) return;

        emitter.WriteSpace().WriteRaw("(");
        for (int index = 0; index < ColumnNames.Count; index++)
        {
            if (index > 0)
                emitter.WriteRaw(", ");

            emitter.WriteIdentifier(ColumnNames[index]);
        }

        emitter.WriteRaw(")")
            .WriteSpace()
            .WriteKeyword(emitter.Dialect.Values)
            .WriteSpace();

        for (int rowIndex = 0; rowIndex < valueRows.Count; rowIndex++)
        {
            if (rowIndex > 0) emitter.WriteRaw(", ");

            emitter.WriteRaw("(");
            List<object> row = valueRows[rowIndex];
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                if (columnIndex > 0)
                    emitter.WriteRaw(", ");

                emitter.WritePlaceholderForValue(row[columnIndex]);
            }

            emitter.WriteRaw(")");
        }
    }
}
