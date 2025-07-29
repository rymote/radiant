using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Expressions;

public enum PatternMatchOperator
{
    Like, // LIKE
    ILike, // ILIKE (case-insensitive)
    NotLike, // NOT LIKE
    NotILike, // NOT ILIKE
    SimilarTo, // SIMILAR TO
    NotSimilarTo, // NOT SIMILAR TO
    RegexMatch, // ~ (regex match)
    RegexIMatch, // ~* (case-insensitive regex)
    NotRegexMatch, // !~ (not regex match)
    NotRegexIMatch // !~* (case-insensitive not regex)
}

public sealed class PatternMatchExpression : ISqlExpression
{
    public ISqlExpression Column { get; }
    public PatternMatchOperator Operator { get; }
    public ISqlExpression Pattern { get; }
    public string? Alias { get; private set; }

    public PatternMatchExpression(ISqlExpression column, PatternMatchOperator op, ISqlExpression pattern)
    {
        Column = column;
        Operator = op;
        Pattern = pattern;
    }

    public PatternMatchExpression As(string alias)
    {
        Alias = alias;
        return this;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        Column.AppendTo(stringBuilder);
        stringBuilder.Append(SqlKeywords.SPACE).Append(GetOperatorString()).Append(SqlKeywords.SPACE);
        Pattern.AppendTo(stringBuilder);

        if (!string.IsNullOrEmpty(Alias))
            stringBuilder
                .Append(SqlKeywords.SPACE).Append(SqlKeywords.AS).Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.QUOTE).Append(Alias).Append(SqlKeywords.QUOTE);
    }

    private string GetOperatorString() => Operator switch
    {
        PatternMatchOperator.Like => SqlKeywords.LIKE,
        PatternMatchOperator.ILike => SqlKeywords.ILIKE,
        PatternMatchOperator.NotLike => SqlKeywords.NOT + SqlKeywords.SPACE + SqlKeywords.LIKE,
        PatternMatchOperator.NotILike => SqlKeywords.NOT + SqlKeywords.SPACE + SqlKeywords.ILIKE,
        PatternMatchOperator.SimilarTo => SqlKeywords.SIMILAR_TO,
        PatternMatchOperator.NotSimilarTo => SqlKeywords.NOT + SqlKeywords.SPACE + SqlKeywords.SIMILAR_TO,
        PatternMatchOperator.RegexMatch => SqlKeywords.REGEXP_MATCH_OP,
        PatternMatchOperator.RegexIMatch => SqlKeywords.REGEXP_IMATCH_OP,
        PatternMatchOperator.NotRegexMatch => SqlKeywords.NOT_REGEXP_MATCH,
        PatternMatchOperator.NotRegexIMatch => SqlKeywords.NOT_REGEXP_IMATCH,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static PatternMatchExpression Like(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.Like, new LiteralExpression(pattern));

    public static PatternMatchExpression ILike(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.ILike, new LiteralExpression(pattern));

    public static PatternMatchExpression NotLike(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.NotLike, new LiteralExpression(pattern));

    public static PatternMatchExpression RegexMatch(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.RegexMatch, new LiteralExpression(pattern));

    public static PatternMatchExpression RegexIMatch(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.RegexIMatch, new LiteralExpression(pattern));

    public static PatternMatchExpression SimilarTo(string column, string pattern) =>
        new(new ColumnExpression(column), PatternMatchOperator.SimilarTo, new LiteralExpression(pattern));
}