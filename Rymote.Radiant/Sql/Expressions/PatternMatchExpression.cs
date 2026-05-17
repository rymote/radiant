using Rymote.Radiant.Sql.Compiler;

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

    public void Accept(SqlEmitter emitter)
    {
        emitter.Emit(Column);
        emitter.WriteSpace().WriteRaw(GetDialectOperator(emitter)).WriteSpace();
        emitter.Emit(Pattern);

        if (!string.IsNullOrEmpty(Alias))
            emitter.WriteSpace().WriteRaw("AS").WriteSpace().WriteIdentifier(Alias);
    }

    private string GetDialectOperator(SqlEmitter emitter) => Operator switch
    {
        PatternMatchOperator.Like => "LIKE",
        PatternMatchOperator.ILike => emitter.Dialect.CaseInsensitiveLikeOperator,
        PatternMatchOperator.NotLike => "NOT LIKE",
        PatternMatchOperator.NotILike => "NOT " + emitter.Dialect.CaseInsensitiveLikeOperator,
        PatternMatchOperator.SimilarTo => "SIMILAR TO",
        PatternMatchOperator.NotSimilarTo => "NOT SIMILAR TO",
        PatternMatchOperator.RegexMatch => "~",
        PatternMatchOperator.RegexIMatch => "~*",
        PatternMatchOperator.NotRegexMatch => "!~",
        PatternMatchOperator.NotRegexIMatch => "!~*",
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