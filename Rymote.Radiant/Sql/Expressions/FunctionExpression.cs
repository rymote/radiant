using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class FunctionExpression : ISqlExpression
{
    public string FunctionName { get; }
    public IReadOnlyList<ISqlExpression> Arguments { get; }
    public string? Alias { get; private set; }

    public FunctionExpression(string functionName, params ISqlExpression[] arguments)
    {
        FunctionName = functionName;
        Arguments = arguments.ToList();
    }

    public FunctionExpression As(string alias)
    {
        Alias = alias;
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

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    public static FunctionExpression Coalesce(params ISqlExpression[] expressions) =>
        new(SqlKeywords.COALESCE, expressions);

    public static FunctionExpression NullIf(ISqlExpression expression1, ISqlExpression expression2) =>
        new(SqlKeywords.NULLIF, expression1, expression2);

    public static FunctionExpression Round(ISqlExpression expression, ISqlExpression? precision = null)
    {
        ISqlExpression[] expressions = precision != null ? [expression, precision] : [expression];
        return new FunctionExpression(SqlKeywords.ROUND, expressions);
    }

    public static FunctionExpression ToChar(ISqlExpression expression, ISqlExpression format) =>
        new(SqlKeywords.TO_CHAR, expression, format);

    public static FunctionExpression Concat(params ISqlExpression[] expressions) =>
        new(SqlKeywords.CONCAT, expressions);

    public static FunctionExpression Upper(ISqlExpression expression) =>
        new(SqlKeywords.UPPER, expression);

    public static FunctionExpression Lower(ISqlExpression expression) =>
        new(SqlKeywords.LOWER, expression);
}