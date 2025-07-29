using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum RangeOperator
{
    Contains, // @> (range contains element/range)
    ContainedBy, // <@ (range contained by)
    Overlap, // && (range overlap)
    StrictlyLeft, // << (strictly left of)
    StrictlyRight, // >> (strictly right of)
    DoesNotExtendRight, // &< (does not extend to the right of)
    DoesNotExtendLeft, // &> (does not extend to the left of)
    Adjacent, // -|- (adjacent)
    Union, // + (range union)
    Intersection, // * (range intersection)
    Difference // - (range difference)
}

public sealed class RangeExpression : ISqlExpression
{
    public ISqlExpression Left { get; }
    public RangeOperator Operator { get; }
    public ISqlExpression Right { get; }
    public string? Alias { get; private set; }

    public RangeExpression(ISqlExpression left, RangeOperator rangeOperator, ISqlExpression right)
    {
        Left = left;
        Operator = rangeOperator;
        Right = right;
    }

    public RangeExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        Left.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorString()).Append(SqlKeywords.SPACE);
        Right.AppendTo(stringBuilder);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    private string GetOperatorString() => Operator switch
    {
        RangeOperator.Contains => SqlKeywords.RANGE_CONTAINS_ELEMENT,
        RangeOperator.ContainedBy => SqlKeywords.RANGE_CONTAINED_BY,
        RangeOperator.Overlap => SqlKeywords.RANGE_OVERLAP,
        RangeOperator.StrictlyLeft => SqlKeywords.RANGE_LEFT_OF,
        RangeOperator.StrictlyRight => SqlKeywords.RANGE_RIGHT_OF,
        RangeOperator.Adjacent => SqlKeywords.RANGE_ADJACENT,
        RangeOperator.Union => SqlKeywords.RANGE_UNION,
        RangeOperator.Intersection => SqlKeywords.RANGE_INTERSECTION,
        RangeOperator.Difference => SqlKeywords.RANGE_DIFFERENCE,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static RangeExpression Contains(string column, object value) =>
        new(new ColumnExpression(column), RangeOperator.Contains, new LiteralExpression(value));

    public static RangeExpression ContainedBy(string column, string range) =>
        new(new ColumnExpression(column), RangeOperator.ContainedBy, new LiteralExpression(range));

    public static RangeExpression Overlaps(string column, string range) =>
        new(new ColumnExpression(column), RangeOperator.Overlap, new LiteralExpression(range));
}

public sealed class RangeLiteralExpression : ISqlExpression
{
    public object? LowerBound { get; }
    public object? UpperBound { get; }
    public bool LowerInclusive { get; }
    public bool UpperInclusive { get; }
    public string RangeType { get; }

    public RangeLiteralExpression(object? lowerBound, object? upperBound,
        bool lowerInclusive = true, bool upperInclusive = false, string rangeType = "int4range")
    {
        LowerBound = lowerBound;
        UpperBound = upperBound;
        LowerInclusive = lowerInclusive;
        UpperInclusive = upperInclusive;
        RangeType = rangeType;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(RangeType).Append(SqlKeywords.OPEN_PAREN);

        if (LowerBound != null)
            stringBuilder.Append(LowerBound);
        else
            stringBuilder.Append(SqlKeywords.NULL);

        stringBuilder.Append(SqlKeywords.COMMA);

        if (UpperBound != null)
            stringBuilder.Append(UpperBound);
        else
            stringBuilder.Append(SqlKeywords.NULL);

        stringBuilder.Append(SqlKeywords.COMMA)
            .Append(SqlKeywords.SINGLE_QUOTE)
            .Append(LowerInclusive ? SqlKeywords.OPEN_BRACKET : SqlKeywords.OPEN_PAREN)
            .Append(UpperInclusive ? SqlKeywords.CLOSE_BRACKET : SqlKeywords.CLOSE_PAREN)
            .Append(SqlKeywords.SINGLE_QUOTE)
            .Append(SqlKeywords.CLOSE_PAREN);
    }

    public static RangeLiteralExpression
        IntRange(int? lower, int? upper, bool lowerInc = true, bool upperInc = false) =>
        new(lower, upper, lowerInc, upperInc, "int4range");

    public static RangeLiteralExpression BigIntRange(long? lower, long? upper, bool lowerInc = true,
        bool upperInc = false) =>
        new(lower, upper, lowerInc, upperInc, "int8range");

    public static RangeLiteralExpression NumRange(decimal? lower, decimal? upper, bool lowerInc = true,
        bool upperInc = false) =>
        new(lower, upper, lowerInc, upperInc, "numrange");

    public static RangeLiteralExpression TsRange(DateTime? lower, DateTime? upper, bool lowerInc = true,
        bool upperInc = false) =>
        new(lower?.ToString("yyyy-MM-dd HH:mm:ss"), upper?.ToString("yyyy-MM-dd HH:mm:ss"), lowerInc, upperInc,
            "tsrange");

    public static RangeLiteralExpression DateRange(DateTime? lower, DateTime? upper, bool lowerInc = true,
        bool upperInc = false) =>
        new(lower?.ToString("yyyy-MM-dd"), upper?.ToString("yyyy-MM-dd"), lowerInc, upperInc, "daterange");
}