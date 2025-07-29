using System.Text;
using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Clauses.WindowFunction;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class WindowFunctionExpression : ISqlExpression
{
    public string FunctionName { get; }
    public IReadOnlyList<ISqlExpression> Arguments { get; }
    public string? Alias { get; private set; }
    
    private readonly List<string> partitionByColumns = [];
    private readonly List<(string Column, SortDirection Direction)> orderByColumns = [];
    private WindowFrameClause? frameClause;
    private bool respectNulls = false;
    private bool ignoreNulls = false;

    public WindowFunctionExpression(string functionName, params ISqlExpression[] arguments)
    {
        FunctionName = functionName;
        Arguments = arguments.ToList();
    }

    public WindowFunctionExpression PartitionBy(params string[] columns)
    {
        partitionByColumns.AddRange(columns);
        return this;
    }

    public WindowFunctionExpression OrderBy(string column, SortDirection direction = SortDirection.Ascending)
    {
        orderByColumns.Add((column, direction));
        return this;
    }

    public WindowFunctionExpression Rows(WindowFrameBoundary start, WindowFrameBoundary? end = null)
    {
        frameClause = new WindowFrameClause(WindowFrameType.Rows, start, end);
        return this;
    }

    public WindowFunctionExpression Range(WindowFrameBoundary start, WindowFrameBoundary? end = null)
    {
        frameClause = new WindowFrameClause(WindowFrameType.Range, start, end);
        return this;
    }

    public WindowFunctionExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public WindowFunctionExpression RespectNulls()
    {
        respectNulls = true;
        return this;
    }

    public WindowFunctionExpression IgnoreNulls()
    {
        ignoreNulls = true;
        return this;
    }
    
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(FunctionName).Append(SqlKeywords.OPEN_PAREN);
        for (int index = 0; index < Arguments.Count; index++)
        {
            if (index > 0) 
                stringBuilder.Append(SqlKeywords.COMMA);
            
            Arguments[index].AppendTo(stringBuilder);
        }
        
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
        
        if (respectNulls)
            stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.RESPECT_NULLS);
        else if (ignoreNulls)
            stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.IGNORE_NULLS);
        
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OVER).Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);

        if (partitionByColumns.Count > 0)
        {
            stringBuilder.Append(SqlKeywords.PARTITION_BY).Append(SqlKeywords.SPACE);
            for (int index = 0; index < partitionByColumns.Count; index++)
            {
                if (index > 0) stringBuilder.Append(SqlKeywords.COMMA);
                stringBuilder.Append(SqlKeywords.QUOTE).Append(partitionByColumns[index]).Append(SqlKeywords.QUOTE);
            }
        }

        if (orderByColumns.Count > 0)
        {
            if (partitionByColumns.Count > 0) stringBuilder.Append(SqlKeywords.SPACE);
            stringBuilder.Append(SqlKeywords.ORDER_BY).Append(SqlKeywords.SPACE);
            
            for (int index = 0; index < orderByColumns.Count; index++)
            {
                if (index > 0) 
                    stringBuilder.Append(SqlKeywords.COMMA);
                
                (string column, SortDirection direction) = orderByColumns[index];
                stringBuilder.Append(SqlKeywords.QUOTE).Append(column).Append(SqlKeywords.QUOTE);
                stringBuilder.Append(direction == SortDirection.Descending ? 
                    SqlKeywords.SPACE + SqlKeywords.DESC : 
                    SqlKeywords.SPACE + SqlKeywords.ASC);
            }
        }

        if (frameClause != null)
        {
            if (partitionByColumns.Count > 0 || orderByColumns.Count > 0) 
                stringBuilder.Append(SqlKeywords.SPACE);
            frameClause.AppendTo(stringBuilder);
        }

        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }
}