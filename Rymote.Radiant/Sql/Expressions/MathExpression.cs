using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum MathOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo
}

public sealed class MathExpression : ISqlExpression
{
    public ISqlExpression Left { get; }
    public MathOperator Operator { get; }
    public ISqlExpression Right { get; }
    public string? Alias { get; private set; }

    public MathExpression(ISqlExpression left, MathOperator mathOperator, ISqlExpression right)
    {
        Left = left;
        Operator = mathOperator;
        Right = right;
    }

    public MathExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.OPEN_PAREN);
        Left.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorSymbol()).Append(SqlKeywords.SPACE);
        Right.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    private string GetOperatorSymbol() => Operator switch
    {
        MathOperator.Add => SqlKeywords.PLUS,
        MathOperator.Subtract => SqlKeywords.MINUS,
        MathOperator.Multiply => SqlKeywords.MULTIPLY,
        MathOperator.Divide => SqlKeywords.DIVIDE,
        MathOperator.Modulo => SqlKeywords.MODULO,
        _ => throw new ArgumentOutOfRangeException()
    };
}