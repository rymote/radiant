using System;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.MySql;

public sealed class MySqlDialect : ISqlDialect
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
    public string ReturningKeyword => throw new NotSupportedException("MySQL has no RETURNING clause; use INSERT followed by SELECT LAST_INSERT_ID() or a raw SQL path.");
    public string OnConflict => throw new NotSupportedException("MySQL uses 'INSERT ... ON DUPLICATE KEY UPDATE' rather than 'ON CONFLICT'; use the raw SQL path for upserts.");
    public string DoNothing => throw new NotSupportedException("MySQL has no 'ON CONFLICT DO NOTHING'; use 'INSERT IGNORE' via the raw SQL path.");
    public string DoUpdate => throw new NotSupportedException("MySQL has no 'ON CONFLICT DO UPDATE'; use 'INSERT ... ON DUPLICATE KEY UPDATE' via the raw SQL path.");
    public string ExcludedTableAlias => throw new NotSupportedException("MySQL has no excluded-row pseudo-table; use 'VALUES(col)' or a row alias in raw SQL.");
    public string CaseInsensitiveLikeOperator => "LIKE";
    public string CurrentTimestampExpression => "CURRENT_TIMESTAMP";
    public string CastOperator => throw new NotSupportedException("MySQL has no '::' cast operator; use CAST(value AS type) instead.");
    public string ConcatenateOperator => throw new NotSupportedException("MySQL '||' is the OR operator by default; use CONCAT(a, b) instead.");
    public string With => "WITH";
    public string Recursive => "RECURSIVE";
    public string LateralKeyword => throw new NotSupportedException("MySQL 8.0.14+ supports LATERAL derived tables but the emission form differs; not supported by this adapter.");
    public string NullLiteral => "NULL";
    public string TrueLiteral => "TRUE";
    public string FalseLiteral => "FALSE";

    public IJsonbDialect Jsonb { get; } = new MySqlJsonDialect();
    public IArrayDialect Array { get; } = new UnsupportedArrayDialect();
    public IVectorDialect Vector { get; } = new UnsupportedVectorDialect();
    public IFullTextDialect FullText { get; } = new MySqlFullTextDialect();
    public IRangeDialect Range { get; } = new UnsupportedRangeDialect();

    private sealed class MySqlJsonDialect : IJsonbDialect
    {
        public string ExtractText => "->>";
        public string ExtractJson => "->";
        public string ContainsOperator => throw new NotSupportedException("MySQL has no JSON 'contains' operator; use JSON_CONTAINS(target, candidate) instead.");
        public string ContainedByOperator => throw new NotSupportedException("MySQL has no JSON 'contained-by' operator; use JSON_CONTAINS(target, candidate) with arguments swapped.");
        public string HasKeyOperator => throw new NotSupportedException("MySQL has no JSON 'has key' operator; use JSON_CONTAINS_PATH(target, 'one', path).");
        public string HasAnyKeyOperator => throw new NotSupportedException("MySQL has no JSON 'has any key' operator; use JSON_CONTAINS_PATH(target, 'one', ...).");
        public string HasAllKeysOperator => throw new NotSupportedException("MySQL has no JSON 'has all keys' operator; use JSON_CONTAINS_PATH(target, 'all', ...).");
        public string PathExistsOperator => throw new NotSupportedException("MySQL has no JSON 'path exists' operator; use JSON_CONTAINS_PATH.");
        public string PathMatchOperator => throw new NotSupportedException("MySQL has no JSON 'path match' operator.");
        public string ConcatenateOperator => throw new NotSupportedException("MySQL has no JSON concatenation operator; use JSON_MERGE_PATCH or JSON_MERGE_PRESERVE.");
        public string DeletePathOperator => throw new NotSupportedException("MySQL has no JSON 'delete path' operator; use JSON_REMOVE.");
    }

    private sealed class UnsupportedArrayDialect : IArrayDialect
    {
        public string ContainsOperator => throw new NotSupportedException("MySQL has no array column type.");
        public string ContainedByOperator => throw new NotSupportedException("MySQL has no array column type.");
        public string OverlapOperator => throw new NotSupportedException("MySQL has no array column type.");
        public string ConcatenateOperator => throw new NotSupportedException("MySQL has no array column type.");
        public string CardinalityFunction => throw new NotSupportedException("MySQL has no array column type.");
        public string UnnestFunction => throw new NotSupportedException("MySQL has no array column type.");
    }

    private sealed class UnsupportedVectorDialect : IVectorDialect
    {
        public string L2DistanceOperator => throw new NotSupportedException("MySQL has no native vector type.");
        public string InnerProductOperator => throw new NotSupportedException("MySQL has no native vector type.");
        public string CosineDistanceOperator => throw new NotSupportedException("MySQL has no native vector type.");
        public string L1DistanceOperator => throw new NotSupportedException("MySQL has no native vector type.");
    }

    private sealed class MySqlFullTextDialect : IFullTextDialect
    {
        // MySQL full-text search uses 'MATCH (col) AGAINST (query)' syntax; the emission form differs from PostgreSQL's '@@'.
        public string MatchOperator => "MATCH";
        public string ToTsVectorFunction => throw new NotSupportedException("MySQL has no tsvector type; full-text search uses FULLTEXT indexes plus MATCH ... AGAINST.");
        public string ToTsQueryFunction => throw new NotSupportedException("MySQL has no tsquery type; full-text search uses MATCH ... AGAINST.");
        public string PlainToTsQueryFunction => throw new NotSupportedException("MySQL has no plainto_tsquery equivalent.");
        public string PhraseToTsQueryFunction => throw new NotSupportedException("MySQL has no phraseto_tsquery equivalent.");
        public string WebSearchToTsQueryFunction => throw new NotSupportedException("MySQL has no websearch_to_tsquery equivalent.");
        public string TsRankFunction => throw new NotSupportedException("MySQL has no ts_rank equivalent; relevance is returned by MATCH ... AGAINST itself.");
        public string TsRankCoverDensityFunction => throw new NotSupportedException("MySQL has no ts_rank_cd equivalent.");
        public string TsHeadlineFunction => throw new NotSupportedException("MySQL has no ts_headline equivalent.");
    }

    private sealed class UnsupportedRangeDialect : IRangeDialect
    {
        public string ContainsElementOperator => throw new NotSupportedException("MySQL has no range type.");
        public string ContainedByOperator => throw new NotSupportedException("MySQL has no range type.");
        public string OverlapOperator => throw new NotSupportedException("MySQL has no range type.");
        public string AdjacentOperator => throw new NotSupportedException("MySQL has no range type.");
    }
}
