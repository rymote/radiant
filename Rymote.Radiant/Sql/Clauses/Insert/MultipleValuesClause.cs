using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (valueRows.Count == 0) return;

        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);
        for (int index = 0; index < ColumnNames.Count; index++)
        {
            if (index > 0) 
                stringBuilder.Append(SqlKeywords.COMMA);
            
            stringBuilder.Append(SqlKeywords.QUOTE).Append(ColumnNames[index]).Append(SqlKeywords.QUOTE);
        }
        
        stringBuilder
            .Append(SqlKeywords.CLOSE_PAREN)
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.VALUES)
            .Append(SqlKeywords.SPACE);

        for (int rowIndex = 0; rowIndex < valueRows.Count; rowIndex++)
        {
            if (rowIndex > 0) stringBuilder.Append(SqlKeywords.COMMA);
            
            stringBuilder.Append(SqlKeywords.OPEN_PAREN);
            List<object> row = valueRows[rowIndex];
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                if (columnIndex > 0) 
                    stringBuilder.Append(SqlKeywords.COMMA);
                
                string parameterName = parameterBag.Add(row[columnIndex]);
                stringBuilder.Append(SqlKeywords.PARAMETER_PREFIX).Append(parameterName);
            }
            
            stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
        }
    }

    public int RowCount => valueRows.Count;
}