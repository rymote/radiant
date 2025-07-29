using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class AggregateExpression : ISqlExpression
{
    public string FunctionName { get; }
    public ISqlExpression InnerExpression { get; }
    public bool IsDistinct { get; }

    public AggregateExpression(string functionName, ISqlExpression innerExpression, bool isDistinct = false)
    {
        FunctionName = functionName;
        InnerExpression = innerExpression;
        IsDistinct = isDistinct;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(FunctionName)
            .Append(SqlKeywords.OPEN_PAREN);
        
        if (IsDistinct) 
            stringBuilder.Append(SqlKeywords.DISTINCT).Append(SqlKeywords.SPACE);
        
        InnerExpression.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
    }
}
