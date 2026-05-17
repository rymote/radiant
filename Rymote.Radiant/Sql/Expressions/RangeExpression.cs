using Rymote.Radiant.Sql.Compiler;

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

    public static RangeExpression Contains(string column, object value) =>
        new(new ColumnExpression(column), RangeOperator.Contains, new LiteralExpression(value));

    public static RangeExpression ContainedBy(string column, string range) =>
        new(new ColumnExpression(column), RangeOperator.ContainedBy, new LiteralExpression(range));

    public static RangeExpression Overlaps(string column, string range) =>
        new(new ColumnExpression(column), RangeOperator.Overlap, new LiteralExpression(range));

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(Left);
        emitter.WriteSpace().WriteRaw(GetDialectOperator(emitter)).WriteSpace();
        emitter.Emit(Right);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }

    private string GetDialectOperator(SqlEmitter emitter) => Operator switch
    {
        RangeOperator.Contains => emitter.Dialect.Range.ContainsElementOperator,
        RangeOperator.ContainedBy => emitter.Dialect.Range.ContainedByOperator,
        RangeOperator.Overlap => emitter.Dialect.Range.OverlapOperator,
        RangeOperator.StrictlyLeft => "<<",
        RangeOperator.StrictlyRight => ">>",
        RangeOperator.Adjacent => emitter.Dialect.Range.AdjacentOperator,
        RangeOperator.Union => "+",
        RangeOperator.Intersection => "*",
        RangeOperator.Difference => "-",
        _ => throw new ArgumentOutOfRangeException()
    };
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

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(RangeType).WriteRaw("(");

        if (LowerBound != null)
            emitter.WritePlaceholderForValue(LowerBound);
        else
            emitter.WriteKeyword(emitter.Dialect.NullLiteral);

        emitter.WriteRaw(", ");

        if (UpperBound != null)
            emitter.WritePlaceholderForValue(UpperBound);
        else
            emitter.WriteKeyword(emitter.Dialect.NullLiteral);

        string boundsToken = (LowerInclusive ? "[" : "(") + (UpperInclusive ? "]" : ")");
        emitter.WriteRaw(", ").WritePlaceholderForValue(boundsToken).WriteRaw(")");
    }
}