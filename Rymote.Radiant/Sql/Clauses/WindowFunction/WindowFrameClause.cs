using System.Text;
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
}