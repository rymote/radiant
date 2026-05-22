using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum ArrayOperator
{
    Contains, // @> (array contains)
    ContainedBy, // <@ (array contained by)
    Overlap, // && (array overlap)
    Concatenate, // || (array concatenation)
    Equal, // = (array equality)
    NotEqual // != (array inequality)
}

public sealed class ArrayExpression : ISqlExpression
{
    public ISqlExpression Left { get; }
    public ArrayOperator Operator { get; }
    public ISqlExpression Right { get; }
    public string? Alias { get; private set; }

    public ArrayExpression(ISqlExpression left, ArrayOperator op, ISqlExpression right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public ArrayExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public static ArrayExpression Contains(string column, object[] array) =>
        Contains(new ColumnExpression(column), array);

    public static ArrayExpression Contains(ISqlExpression column, object[] array) =>
        new(column, ArrayOperator.Contains, new ArrayLiteralExpression(array));

    public static ArrayExpression ContainedBy(string column, object[] array) =>
        ContainedBy(new ColumnExpression(column), array);

    public static ArrayExpression ContainedBy(ISqlExpression column, object[] array) =>
        new(column, ArrayOperator.ContainedBy, new ArrayLiteralExpression(array));

    public static ArrayExpression Overlap(string column, object[] array) =>
        Overlap(new ColumnExpression(column), array);

    public static ArrayExpression Overlap(ISqlExpression column, object[] array) =>
        new(column, ArrayOperator.Overlap, new ArrayLiteralExpression(array));

    public static ArrayExpression Concatenate(string column, object[] array) =>
        Concatenate(new ColumnExpression(column), array);

    public static ArrayExpression Concatenate(ISqlExpression column, object[] array) =>
        new(column, ArrayOperator.Concatenate, new ArrayLiteralExpression(array));

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
        ArrayOperator.Contains => emitter.Dialect.Array.ContainsOperator,
        ArrayOperator.ContainedBy => emitter.Dialect.Array.ContainedByOperator,
        ArrayOperator.Overlap => emitter.Dialect.Array.OverlapOperator,
        ArrayOperator.Concatenate => emitter.Dialect.Array.ConcatenateOperator,
        ArrayOperator.Equal => "=",
        ArrayOperator.NotEqual => "!=",
        _ => throw new ArgumentOutOfRangeException()
    };
}

public sealed class ArrayLiteralExpression : ISqlExpression
{
    public Array Value { get; }

    public ArrayLiteralExpression(Array value)
    {
        Value = value;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WritePlaceholderForValue(Value);
    }
}

public sealed class ArrayFunctionExpression : ISqlExpression
{
    public string FunctionName { get; }
    public ISqlExpression[] Arguments { get; }
    public string? Alias { get; private set; }

    public ArrayFunctionExpression(string functionName, params ISqlExpression[] arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }

    public ArrayFunctionExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public static ArrayFunctionExpression ArrayLength(ISqlExpression array, int dimension = 1) =>
        new(SqlKeywords.ARRAY_LENGTH, array, new LiteralExpression(dimension));

    public static ArrayFunctionExpression ArrayPosition(ISqlExpression array, ISqlExpression element) =>
        new(SqlKeywords.ARRAY_POSITION, array, element);

    public static ArrayFunctionExpression ArrayAppend(ISqlExpression array, ISqlExpression element) =>
        new(SqlKeywords.ARRAY_APPEND, array, element);

    public static ArrayFunctionExpression ArrayPrepend(ISqlExpression element, ISqlExpression array) =>
        new(SqlKeywords.ARRAY_PREPEND, element, array);

    public static ArrayFunctionExpression ArrayRemove(ISqlExpression array, ISqlExpression element) =>
        new(SqlKeywords.ARRAY_REMOVE, array, element);

    public static ArrayFunctionExpression ArrayToString(ISqlExpression array, string delimiter,
        string nullString = "NULL") =>
        new(SqlKeywords.ARRAY_TO_STRING, array, new LiteralExpression(delimiter), new LiteralExpression(nullString));

    public static ArrayFunctionExpression StringToArray(ISqlExpression str, string delimiter,
        string nullString = "NULL") =>
        new(SqlKeywords.STRING_TO_ARRAY, str, new LiteralExpression(delimiter), new LiteralExpression(nullString));

    public static ArrayFunctionExpression Cardinality(ISqlExpression array) =>
        new(SqlKeywords.CARDINALITY, array);

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(FunctionName).WriteRaw("(");

        for (int index = 0; index < Arguments.Length; index++)
        {
            if (index > 0) emitter.WriteRaw(", ");
            emitter.Emit(Arguments[index]);
        }

        emitter.WriteRaw(")");

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }
}