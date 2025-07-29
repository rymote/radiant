using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum JsonbOperator
{
    PathExists, // @?
    PathMatch, // @@
    DeletePath, // #-
    ConcatPath, // ||

    StrictContains, // @>
    StrictContainedBy, // <@
    HasKey, // ?
    HasAnyKey, // ?|
    HasAllKeys // ?&
}

public sealed class JsonbExpression : ISqlExpression
{
    public ISqlExpression JsonColumn { get; }
    public JsonbOperator Operator { get; }
    public ISqlExpression Value { get; }

    public JsonbExpression(ISqlExpression jsonColumn, JsonbOperator op, ISqlExpression value)
    {
        JsonColumn = jsonColumn;
        Operator = op;
        Value = value;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        JsonColumn.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorSymbol()).Append(SqlKeywords.SPACE);
        Value.AppendTo(stringBuilder);
    }
    
    private string GetOperatorSymbol() => Operator switch
    {
        JsonbOperator.PathExists => SqlKeywords.JSONB_PATH_EXISTS,
        JsonbOperator.PathMatch => SqlKeywords.JSONB_PATH_MATCH,
        JsonbOperator.DeletePath => SqlKeywords.JSONB_DELETE_PATH,
        JsonbOperator.ConcatPath => SqlKeywords.JSONB_CONCAT,
        JsonbOperator.StrictContains => SqlKeywords.JSONB_STRICT_CONTAINS,
        JsonbOperator.StrictContainedBy => SqlKeywords.JSONB_STRICT_CONTAINED,
        JsonbOperator.HasKey => SqlKeywords.JSONB_HAS_KEY,
        JsonbOperator.HasAnyKey => SqlKeywords.JSONB_HAS_ANY_KEY,
        JsonbOperator.HasAllKeys => SqlKeywords.JSONB_HAS_ALL_KEYS,
        _ => throw new ArgumentException("Invalid JSONB operator")
    };

    public static JsonbExpression PathExists(string column, string jsonPath) =>
        new(new ColumnExpression(column), JsonbOperator.PathExists, new LiteralExpression(jsonPath));

    public static JsonbExpression PathQuery(string column, string jsonPath) =>
        new(new FunctionExpression("jsonb_path_query", new ColumnExpression(column), new LiteralExpression(jsonPath)),
            JsonbOperator.StrictContains, new LiteralExpression("{}"));
}