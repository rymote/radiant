using Rymote.Radiant.Sql.Compiler;
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

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
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