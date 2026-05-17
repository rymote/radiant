using System.Text;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Smart.Query;

public static class SmartQueryExtensions
{
    public static SelectBuilder WhereExpression(this SelectBuilder selectBuilder, IDatabaseAdapter adapter, ISqlExpression expression, string operatorSymbol, object value)
    {
        string expressionString = BuildExpressionString(adapter, expression);
        return selectBuilder.Where(expressionString, operatorSymbol, value);
    }

    public static SelectBuilder OrderByExpression(this SelectBuilder selectBuilder, IDatabaseAdapter adapter, ISqlExpression expression, Sql.Clauses.OrderBy.SortDirection sortDirection)
    {
        string expressionString = BuildExpressionString(adapter, expression);
        return selectBuilder.OrderBy(expressionString, sortDirection);
    }

    public static string BuildExpressionString(IDatabaseAdapter adapter, ISqlExpression expression)
    {
        StringBuilder stringBuilder = new StringBuilder();
        ParameterBag parameterBag = new ParameterBag(adapter.ParameterFormatter);
        SqlEmitter emitter = new SqlEmitter(adapter, stringBuilder, parameterBag);
        expression.Accept(emitter);
        return stringBuilder.ToString();
    }
}
