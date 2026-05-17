using Rymote.Radiant.Sql.Compiler;

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

    public static JsonbExpression PathExists(string column, string jsonPath) =>
        new(new ColumnExpression(column), JsonbOperator.PathExists, new LiteralExpression(jsonPath));

    public static JsonbExpression PathQuery(string column, string jsonPath) =>
        new(new FunctionExpression("jsonb_path_query", new ColumnExpression(column), new LiteralExpression(jsonPath)),
            JsonbOperator.StrictContains, new LiteralExpression("{}"));

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(JsonColumn);
        emitter.WriteSpace().WriteRaw(GetDialectOperator(emitter)).WriteSpace();
        emitter.Emit(Value);
    }

    private string GetDialectOperator(SqlEmitter emitter) => Operator switch
    {
        JsonbOperator.PathExists => emitter.Dialect.Jsonb.PathExistsOperator,
        JsonbOperator.PathMatch => emitter.Dialect.Jsonb.PathMatchOperator,
        JsonbOperator.DeletePath => emitter.Dialect.Jsonb.DeletePathOperator,
        JsonbOperator.ConcatPath => emitter.Dialect.Jsonb.ConcatenateOperator,
        JsonbOperator.StrictContains => emitter.Dialect.Jsonb.ContainsOperator,
        JsonbOperator.StrictContainedBy => emitter.Dialect.Jsonb.ContainedByOperator,
        JsonbOperator.HasKey => emitter.Dialect.Jsonb.HasKeyOperator,
        JsonbOperator.HasAnyKey => emitter.Dialect.Jsonb.HasAnyKeyOperator,
        JsonbOperator.HasAllKeys => emitter.Dialect.Jsonb.HasAllKeysOperator,
        _ => throw new ArgumentException("Invalid JSONB operator")
    };
}