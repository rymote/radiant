using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Smart.Query;

public static class SmartQueryExtensions
{
    public static SelectBuilder WhereExpression(this SelectBuilder selectBuilder, ISqlExpression expression, string operatorSymbol, object value)
    {
        StringBuilder stringBuilder = new StringBuilder();
        expression.AppendTo(stringBuilder);
        string expressionString = stringBuilder.ToString();
        
        return selectBuilder.Where(expressionString, operatorSymbol, value);
    }

    public static SelectBuilder OrderByExpression(this SelectBuilder selectBuilder, ISqlExpression expression, Sql.Clauses.OrderBy.SortDirection sortDirection)
    {
        StringBuilder stringBuilder = new StringBuilder();
        expression.AppendTo(stringBuilder);
        string expressionString = stringBuilder.ToString();
        
        return selectBuilder.OrderBy(expressionString, sortDirection);
    }

    public static string BuildExpressionString(ISqlExpression expression)
    {
        StringBuilder stringBuilder = new StringBuilder();
        expression.AppendTo(stringBuilder);
        return stringBuilder.ToString();
    }
} 