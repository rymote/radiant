using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public sealed class FullTextExpression : ISqlExpression
{
    public string FunctionName { get; }
    public ISqlExpression[] Arguments { get; }
    
    public FullTextExpression(string functionName, params ISqlExpression[] arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
    
    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(FunctionName).Append(SqlKeywords.OPEN_PAREN);
        for (int index = 0; index < Arguments.Length; index++)
        {
            if (index > 0) 
                stringBuilder.Append(SqlKeywords.COMMA);
            
            Arguments[index].AppendTo(stringBuilder);
        }
        
        stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
    }
    
    public static FullTextExpression ToTsVector(string config, ISqlExpression text) =>
        new(SqlKeywords.FTS_TO_TSVECTOR, new LiteralExpression(config), text);
        
    public static FullTextExpression ToTsQuery(string config, string query) =>
        new(SqlKeywords.FTS_TO_TSQUERY, new LiteralExpression(config), new LiteralExpression(query));
        
    public static FullTextExpression PlainToTsQuery(string config, string query) =>
        new(SqlKeywords.FTS_PLAINTO_TSQUERY, new LiteralExpression(config), new LiteralExpression(query));
        
    public static FullTextExpression TsRank(ISqlExpression vector, ISqlExpression query) =>
        new(SqlKeywords.FTS_TS_RANK, vector, query);
        
    public static FullTextExpression TsHeadline(string config, ISqlExpression text, ISqlExpression query) =>
        new(SqlKeywords.FTS_TS_HEADLINE, new LiteralExpression(config), text, query);

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteRaw(GetDialectFunctionName(emitter)).WriteRaw("(");
        for (int index = 0; index < Arguments.Length; index++)
        {
            if (index > 0)
                emitter.WriteRaw(", ");

            emitter.Emit(Arguments[index]);
        }

        emitter.WriteRaw(")");
    }

    private string GetDialectFunctionName(SqlEmitter emitter) => FunctionName switch
    {
        SqlKeywords.FTS_TO_TSVECTOR => emitter.Dialect.FullText.ToTsVectorFunction,
        SqlKeywords.FTS_TO_TSQUERY => emitter.Dialect.FullText.ToTsQueryFunction,
        SqlKeywords.FTS_PLAINTO_TSQUERY => emitter.Dialect.FullText.PlainToTsQueryFunction,
        SqlKeywords.FTS_PHRASETO_TSQUERY => emitter.Dialect.FullText.PhraseToTsQueryFunction,
        SqlKeywords.FTS_WEBSEARCH_TO_TSQUERY => emitter.Dialect.FullText.WebSearchToTsQueryFunction,
        SqlKeywords.FTS_TS_RANK => emitter.Dialect.FullText.TsRankFunction,
        SqlKeywords.FTS_TS_RANK_CD => emitter.Dialect.FullText.TsRankCoverDensityFunction,
        SqlKeywords.FTS_TS_HEADLINE => emitter.Dialect.FullText.TsHeadlineFunction,
        _ => FunctionName
    };
}

public sealed class FullTextMatchExpression : ISqlExpression
{
    public ISqlExpression TsVector { get; }
    public ISqlExpression TsQuery { get; }
    
    public FullTextMatchExpression(ISqlExpression tsVector, ISqlExpression tsQuery)
    {
        TsVector = tsVector;
        TsQuery = tsQuery;
    }
    
    public void AppendTo(StringBuilder stringBuilder)
    {
        TsVector.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.FTS_MATCH).Append(SqlKeywords.SPACE);
        TsQuery.AppendTo(stringBuilder);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(TsVector);
        emitter.WriteSpace().WriteRaw(emitter.Dialect.FullText.MatchOperator).WriteSpace();
        emitter.Emit(TsQuery);
    }
}