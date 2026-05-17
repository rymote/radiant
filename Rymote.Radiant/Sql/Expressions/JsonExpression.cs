using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;

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

    public void AppendTo(StringBuilder stringBuilder)
    {
        JsonColumn.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorSymbol()).Append(SqlKeywords.SPACE);
        stringBuilder.Append(SqlKeywords.SINGLE_QUOTE).Append(JsonPath).Append(SqlKeywords.SINGLE_QUOTE);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    private string GetOperatorSymbol() => Operator switch
    {
        JsonOperator.ExtractText => SqlKeywords.JSON_EXTRACT_TEXT,
        JsonOperator.ExtractJson => SqlKeywords.JSON_EXTRACT_JSON,
        JsonOperator.PathExists => SqlKeywords.JSON_PATH_EXISTS,
        JsonOperator.ContainsKey => SqlKeywords.JSON_CONTAINS_KEY,
        JsonOperator.Contains => SqlKeywords.JSON_CONTAINS,
        JsonOperator.ContainedBy => SqlKeywords.JSON_CONTAINED_BY,
        _ => throw new ArgumentOutOfRangeException()
    };

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