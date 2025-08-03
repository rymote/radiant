using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Where;

public sealed class WhereCondition : IWhereExpression
{
    public string ColumnName { get; }
    public string OperatorSymbol { get; }
    public object Value { get; }

    public WhereCondition(string columnName, string operatorSymbol, object value)
    {
        ColumnName = columnName;
        OperatorSymbol = operatorSymbol;
        Value = value;
    }
    
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.QUOTE).Append(ColumnName).Append(SqlKeywords.QUOTE)
            .Append(SqlKeywords.SPACE).Append(OperatorSymbol).Append(SqlKeywords.SPACE);

        if ((OperatorSymbol.Equals("IS", StringComparison.OrdinalIgnoreCase) || 
             OperatorSymbol.Equals("IS NOT", StringComparison.OrdinalIgnoreCase)) && 
            Value == null)
        {
            stringBuilder.Append(SqlKeywords.NULL);
        }
        else if ((OperatorSymbol.Equals("IN", StringComparison.OrdinalIgnoreCase) || 
                  OperatorSymbol.Equals("NOT IN", StringComparison.OrdinalIgnoreCase)) && 
                 Value is Array array)
        {
            stringBuilder.Append(SqlKeywords.OPEN_PAREN);
            
            for (int index = 0; index < array.Length; index++)
            {
                if (index > 0)
                    stringBuilder.Append(SqlKeywords.COMMA);
                
                object? element = array.GetValue(index);
                string parameterName = parameterBag.Add(element);
                stringBuilder.Append(SqlKeywords.PARAMETER_PREFIX).Append(parameterName);
            }
            
            stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
        }
        else
        {
            string parameterName = parameterBag.Add(Value);
            stringBuilder.Append(SqlKeywords.PARAMETER_PREFIX).Append(parameterName);
        }
    }
}