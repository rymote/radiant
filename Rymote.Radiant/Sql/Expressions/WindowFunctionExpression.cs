using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Clauses.WindowFunction;
using Rymote.Radiant.Sql.Compiler;

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
    
    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(FunctionName).WriteRaw("(");
        for (int index = 0; index < Arguments.Count; index++)
        {
            if (index > 0)
                emitter.WriteRaw(", ");

            emitter.Emit(Arguments[index]);
        }

        emitter.WriteRaw(")");

        if (respectNulls)
            emitter.WriteSpace().WriteRaw("RESPECT NULLS");
        else if (ignoreNulls)
            emitter.WriteSpace().WriteRaw("IGNORE NULLS");

        emitter.WriteSpace().WriteRaw("OVER").WriteSpace().WriteRaw("(");

        if (partitionByColumns.Count > 0)
        {
            emitter.WriteRaw("PARTITION BY").WriteSpace();
            for (int index = 0; index < partitionByColumns.Count; index++)
            {
                if (index > 0) emitter.WriteRaw(", ");
                emitter.WriteIdentifier(partitionByColumns[index]);
            }
        }

        if (orderByColumns.Count > 0)
        {
            if (partitionByColumns.Count > 0) emitter.WriteSpace();
            emitter.WriteKeyword(emitter.Dialect.OrderBy).WriteSpace();

            for (int index = 0; index < orderByColumns.Count; index++)
            {
                if (index > 0)
                    emitter.WriteRaw(", ");

                (string column, SortDirection direction) = orderByColumns[index];
                emitter.WriteIdentifier(column).WriteSpace().WriteKeyword(direction == SortDirection.Descending
                    ? emitter.Dialect.Descending
                    : emitter.Dialect.Ascending);
            }
        }

        if (frameClause != null)
        {
            if (partitionByColumns.Count > 0 || orderByColumns.Count > 0)
                emitter.WriteSpace();
            frameClause.Accept(emitter);
        }

        emitter.WriteRaw(")");

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }
}