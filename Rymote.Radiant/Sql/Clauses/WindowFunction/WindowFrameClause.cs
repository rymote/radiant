using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Clauses.WindowFunction;

public sealed class WindowFrameClause
{
    public WindowFrameType FrameType { get; }
    public WindowFrameBoundary Start { get; }
    public WindowFrameBoundary? End { get; }
    public int? PrecedingValue { get; }
    public int? FollowingValue { get; }

    public WindowFrameClause(WindowFrameType frameType, WindowFrameBoundary start, WindowFrameBoundary? end = null)
    {
        FrameType = frameType;
        Start = start;
        End = end;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder
            .Append(FrameType == WindowFrameType.Rows ? SqlKeywords.ROWS : SqlKeywords.RANGE)
            .Append(SqlKeywords.SPACE);

        if (End.HasValue)
        {
            stringBuilder
                .Append(SqlKeywords.BETWEEN)
                .Append(SqlKeywords.SPACE);
            
            AppendBoundary(stringBuilder, Start);
            
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.AND)
                .Append(SqlKeywords.SPACE);
            
            AppendBoundary(stringBuilder, End.Value);
        }
        else
        {
            AppendBoundary(stringBuilder, Start);
        }
    }

    private static void AppendBoundary(StringBuilder stringBuilder, WindowFrameBoundary boundary)
    {
        switch (boundary)
        {
            case WindowFrameBoundary.UnboundedPreceding:
                stringBuilder.Append(SqlKeywords.UNBOUNDED_PRECEDING);
                break;

            case WindowFrameBoundary.Preceding:
                stringBuilder.Append(SqlKeywords.PRECEDING);
                break;

            case WindowFrameBoundary.CurrentRow:
                stringBuilder.Append(SqlKeywords.CURRENT_ROW);
                break;

            case WindowFrameBoundary.Following:
                stringBuilder.Append(SqlKeywords.FOLLOWING);
                break;

            case WindowFrameBoundary.UnboundedFollowing:
                stringBuilder.Append(SqlKeywords.UNBOUNDED_FOLLOWING);
                break;
        }
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(FrameType == WindowFrameType.Rows ? "ROWS" : "RANGE").WriteSpace();

        if (End.HasValue)
        {
            emitter.WriteRaw("BETWEEN").WriteSpace();

            EmitBoundary(emitter, Start);

            emitter.WriteSpace().WriteRaw("AND").WriteSpace();

            EmitBoundary(emitter, End.Value);
        }
        else
        {
            EmitBoundary(emitter, Start);
        }
    }

    private static void EmitBoundary(SqlEmitter emitter, WindowFrameBoundary boundary)
    {
        switch (boundary)
        {
            case WindowFrameBoundary.UnboundedPreceding:
                emitter.WriteRaw("UNBOUNDED PRECEDING");
                break;

            case WindowFrameBoundary.Preceding:
                emitter.WriteRaw("PRECEDING");
                break;

            case WindowFrameBoundary.CurrentRow:
                emitter.WriteRaw("CURRENT ROW");
                break;

            case WindowFrameBoundary.Following:
                emitter.WriteRaw("FOLLOWING");
                break;

            case WindowFrameBoundary.UnboundedFollowing:
                emitter.WriteRaw("UNBOUNDED FOLLOWING");
                break;
        }
    }
}