using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.PostgreSql;

public sealed class PostgreSqlDialect : ISqlDialect
{
    public string Select => "SELECT";
    public string From => "FROM";
    public string Where => "WHERE";
    public string GroupBy => "GROUP BY";
    public string Having => "HAVING";
    public string OrderBy => "ORDER BY";
    public string Ascending => "ASC";
    public string Descending => "DESC";
    public string Limit => "LIMIT";
    public string Offset => "OFFSET";
    public string InsertInto => "INSERT INTO";
    public string Update => "UPDATE";
    public string Delete => "DELETE";
    public string Set => "SET";
    public string Values => "VALUES";
    public string ReturningKeyword => "RETURNING";
    public string OnConflict => "ON CONFLICT";
    public string DoNothing => "DO NOTHING";
    public string DoUpdate => "DO UPDATE";
    public string ExcludedTableAlias => "EXCLUDED";
    public string CaseInsensitiveLikeOperator => "ILIKE";
    public string CurrentTimestampExpression => "CURRENT_TIMESTAMP";
    public string CastOperator => "::";
    public string ConcatenateOperator => "||";
    public string With => "WITH";
    public string Recursive => "RECURSIVE";
    public string LateralKeyword => "LATERAL";
    public string NullLiteral => "NULL";
    public string TrueLiteral => "TRUE";
    public string FalseLiteral => "FALSE";

    public IJsonbDialect Jsonb { get; } = new PostgreSqlJsonbDialect();
    public IArrayDialect Array { get; } = new PostgreSqlArrayDialect();
    public IVectorDialect Vector { get; } = new PostgreSqlVectorDialect();
    public IFullTextDialect FullText { get; } = new PostgreSqlFullTextDialect();
    public IRangeDialect Range { get; } = new PostgreSqlRangeDialect();

    private sealed class PostgreSqlJsonbDialect : IJsonbDialect
    {
        public string ExtractText => "->>";
        public string ExtractJson => "->";
        public string ContainsOperator => "@>";
        public string ContainedByOperator => "<@";
        public string HasKeyOperator => "?";
        public string HasAnyKeyOperator => "?|";
        public string HasAllKeysOperator => "?&";
        public string PathExistsOperator => "@?";
        public string PathMatchOperator => "@@";
        public string ConcatenateOperator => "||";
        public string DeletePathOperator => "#-";
    }

    private sealed class PostgreSqlArrayDialect : IArrayDialect
    {
        public string ContainsOperator => "@>";
        public string ContainedByOperator => "<@";
        public string OverlapOperator => "&&";
        public string ConcatenateOperator => "||";
        public string CardinalityFunction => "cardinality";
        public string UnnestFunction => "unnest";
    }

    private sealed class PostgreSqlVectorDialect : IVectorDialect
    {
        public string L2DistanceOperator => "<->";
        public string InnerProductOperator => "<#>";
        public string CosineDistanceOperator => "<=>";
        public string L1DistanceOperator => "<+>";
    }

    private sealed class PostgreSqlFullTextDialect : IFullTextDialect
    {
        public string MatchOperator => "@@";
        public string ToTsVectorFunction => "to_tsvector";
        public string ToTsQueryFunction => "to_tsquery";
        public string PlainToTsQueryFunction => "plainto_tsquery";
        public string PhraseToTsQueryFunction => "phraseto_tsquery";
        public string WebSearchToTsQueryFunction => "websearch_to_tsquery";
        public string TsRankFunction => "ts_rank";
        public string TsRankCoverDensityFunction => "ts_rank_cd";
        public string TsHeadlineFunction => "ts_headline";
    }

    private sealed class PostgreSqlRangeDialect : IRangeDialect
    {
        public string ContainsElementOperator => "@>";
        public string ContainedByOperator => "<@";
        public string OverlapOperator => "&&";
        public string AdjacentOperator => "-|-";
    }
}
