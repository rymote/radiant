using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class LiteralExpression : ISqlExpression
{
    public object Value { get; }

    public LiteralExpression(object value)
    {
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        switch (Value)
        {
            case string str:
                stringBuilder.Append(SqlKeywords.SINGLE_QUOTE)
                    .Append(str.Replace(SqlKeywords.SINGLE_QUOTE, SqlKeywords.SINGLE_QUOTE + SqlKeywords.SINGLE_QUOTE))
                    .Append(SqlKeywords.SINGLE_QUOTE);
                break;
            
            case int or long or decimal or double or float:
                stringBuilder.Append(Value);
                break;
            
            case bool boolean:
                stringBuilder.Append(boolean ? SqlKeywords.TRUE : SqlKeywords.FALSE);
                break;
            
            case null:
                stringBuilder.Append(SqlKeywords.NULL);
                break;
            
            default:
                stringBuilder.Append(SqlKeywords.SINGLE_QUOTE)
                    .Append(Value.ToString()?.Replace(SqlKeywords.SINGLE_QUOTE, SqlKeywords.SINGLE_QUOTE + SqlKeywords.SINGLE_QUOTE))
                    .Append(SqlKeywords.SINGLE_QUOTE);
                break;
        }
    }
    
    public static LiteralExpression String(string value) => new(value);
    public static LiteralExpression Number(int value) => new(value);
    public static LiteralExpression Number(decimal value) => new(value);
    public static LiteralExpression Boolean(bool value) => new(value);
    public static LiteralExpression Null() => new(null!);
}