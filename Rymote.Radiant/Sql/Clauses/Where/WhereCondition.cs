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
        string parameterName = parameterBag.Add(Value);
        
        stringBuilder
            .Append(SqlKeywords.QUOTE).Append(ColumnName).Append(SqlKeywords.QUOTE)
            .Append(SqlKeywords.SPACE).Append(OperatorSymbol).Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.PARAMETER_PREFIX).Append(parameterName);
    }
}