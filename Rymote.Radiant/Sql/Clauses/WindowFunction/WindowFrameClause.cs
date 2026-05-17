using Rymote.Radiant.Sql.Compiler;

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
