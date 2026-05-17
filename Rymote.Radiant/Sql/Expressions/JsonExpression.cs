using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Expressions;

public enum JsonOperator
{
    ExtractText, // ->>
    ExtractJson, // ->
    PathExists, // ?
    ContainsKey, // ?&
    Contains, // @>
    ContainedBy // <@
}

public sealed class JsonExpression : ISqlExpression
{
    public ISqlExpression JsonColumn { get; }
    public string JsonPath { get; }
    public JsonOperator Operator { get; }
    public string? Alias { get; private set; }

    public JsonExpression(ISqlExpression jsonColumn, JsonOperator jsonOperator, string jsonPath)
    {
        JsonColumn = jsonColumn;
        Operator = jsonOperator;
        JsonPath = jsonPath;
    }

    public JsonExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(JsonColumn);
        emitter.WriteSpace().WriteRaw(GetDialectOperator(emitter)).WriteSpace();
        emitter.WritePlaceholderForValue(JsonPath);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }

    private string GetDialectOperator(SqlEmitter emitter) => Operator switch
    {
        JsonOperator.ExtractText => emitter.Dialect.Jsonb.ExtractText,
        JsonOperator.ExtractJson => emitter.Dialect.Jsonb.ExtractJson,
        JsonOperator.PathExists => emitter.Dialect.Jsonb.HasKeyOperator,
        JsonOperator.ContainsKey => emitter.Dialect.Jsonb.HasAllKeysOperator,
        JsonOperator.Contains => emitter.Dialect.Jsonb.ContainsOperator,
        JsonOperator.ContainedBy => emitter.Dialect.Jsonb.ContainedByOperator,
        _ => throw new ArgumentOutOfRangeException()
    };
}
