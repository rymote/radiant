using System.Text;
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
        ArrayOperator.Contains => SqlKeywords.ARRAY_CONTAINS,
        ArrayOperator.ContainedBy => SqlKeywords.ARRAY_CONTAINED_BY,
        ArrayOperator.Overlap => SqlKeywords.ARRAY_OVERLAP,
        ArrayOperator.Concatenate => SqlKeywords.ARRAY_CONCAT,
        ArrayOperator.Equal => SqlKeywords.EQUALS.Trim(),
        ArrayOperator.NotEqual => SqlKeywords.NOT_EQUALS.Trim(),
        _ => throw new ArgumentOutOfRangeException()
    };

    public static ArrayExpression Contains(string column, object[] array) =>
        new(new ColumnExpression(column), ArrayOperator.Contains, new ArrayLiteralExpression(array));

    public static ArrayExpression ContainedBy(string column, object[] array) =>
        new(new ColumnExpression(column), ArrayOperator.ContainedBy, new ArrayLiteralExpression(array));

    public static ArrayExpression Overlap(string column, object[] array) =>
        new(new ColumnExpression(column), ArrayOperator.Overlap, new ArrayLiteralExpression(array));

    public static ArrayExpression Concatenate(string column, object[] array) =>
        new(new ColumnExpression(column), ArrayOperator.Concatenate, new ArrayLiteralExpression(array));
}

public sealed class ArrayLiteralExpression : ISqlExpression
{
    public object[] Values { get; }

    public ArrayLiteralExpression(object[] values)
    {
        Values = values;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.ARRAY).Append(SqlKeywords.OPEN_BRACKET);

        for (int index = 0; index < Values.Length; index++)
        {
            if (index > 0) stringBuilder.Append(SqlKeywords.COMMA);

            switch (Values[index])
            {
                case string str:
                    stringBuilder.Append(SqlKeywords.SINGLE_QUOTE).Append(str).Append(SqlKeywords.SINGLE_QUOTE);
                    break;

                case null:
                    stringBuilder.Append(SqlKeywords.NULL);
                    break;

                default:
                    stringBuilder.Append(Values[index]);
                    break;
            }
        }

        stringBuilder.Append(SqlKeywords.CLOSE_BRACKET);
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

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(FunctionName).Append(SqlKeywords.OPEN_PAREN);

        for (int index = 0; index < Arguments.Length; index++)
        {
            if (index > 0) stringBuilder.Append(SqlKeywords.COMMA);
            Arguments[index].AppendTo(stringBuilder);
        }

        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
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
}