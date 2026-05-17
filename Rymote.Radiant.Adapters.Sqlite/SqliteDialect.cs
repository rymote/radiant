using System;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.Sqlite;

public sealed class SqliteDialect : ISqlDialect
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
    public string ReturningKeyword => "RETURNING";       // SQLite 3.35+
    public string OnConflict => "ON CONFLICT";
    public string DoNothing => "DO NOTHING";
    public string DoUpdate => "DO UPDATE";
    public string ExcludedTableAlias => "excluded";
    public string CaseInsensitiveLikeOperator => "LIKE"; // SQLite LIKE is case-insensitive by default for ASCII
    public string CurrentTimestampExpression => "CURRENT_TIMESTAMP";
    public string CastOperator => "::";                  // SQLite doesn't natively support :: but it parses as a label
    public string ConcatenateOperator => "||";
    public string With => "WITH";
    public string Recursive => "RECURSIVE";
    public string LateralKeyword => throw new NotSupportedException("SQLite does not support LATERAL joins.");
    public string NullLiteral => "NULL";
    public string TrueLiteral => "1";
    public string FalseLiteral => "0";

    public IJsonbDialect Jsonb { get; } = new SqliteJsonDialect();
    public IArrayDialect Array { get; } = new UnsupportedArrayDialect();
    public IVectorDialect Vector { get; } = new UnsupportedVectorDialect();
    public IFullTextDialect FullText { get; } = new SqliteFullTextDialect();
    public IRangeDialect Range { get; } = new UnsupportedRangeDialect();

    private sealed class SqliteJsonDialect : IJsonbDialect
    {
        public string ExtractText => "->>";
        public string ExtractJson => "->";
        public string ContainsOperator => throw new NotSupportedException("SQLite has no JSON 'contains' operator; use json_extract function calls instead.");
        public string ContainedByOperator => throw new NotSupportedException("SQLite has no JSON 'contained-by' operator.");
        public string HasKeyOperator => throw new NotSupportedException("SQLite has no JSON 'has key' operator; use json_extract IS NOT NULL.");
        public string HasAnyKeyOperator => throw new NotSupportedException("SQLite has no JSON 'has any key' operator.");
        public string HasAllKeysOperator => throw new NotSupportedException("SQLite has no JSON 'has all keys' operator.");
        public string PathExistsOperator => throw new NotSupportedException("SQLite has no JSON 'path exists' operator.");
        public string PathMatchOperator => throw new NotSupportedException("SQLite has no JSON 'path match' operator.");
        public string ConcatenateOperator => throw new NotSupportedException("SQLite has no JSON concatenation operator; use json_patch.");
        public string DeletePathOperator => throw new NotSupportedException("SQLite has no JSON 'delete path' operator; use json_remove.");
    }

    private sealed class UnsupportedArrayDialect : IArrayDialect
    {
        public string ContainsOperator => throw new NotSupportedException("SQLite has no array column type.");
        public string ContainedByOperator => throw new NotSupportedException("SQLite has no array column type.");
        public string OverlapOperator => throw new NotSupportedException("SQLite has no array column type.");
        public string ConcatenateOperator => throw new NotSupportedException("SQLite has no array column type.");
        public string CardinalityFunction => throw new NotSupportedException("SQLite has no array column type.");
        public string UnnestFunction => throw new NotSupportedException("SQLite has no array column type.");
    }

    private sealed class UnsupportedVectorDialect : IVectorDialect
    {
        public string L2DistanceOperator => throw new NotSupportedException("SQLite has no native vector type.");
        public string InnerProductOperator => throw new NotSupportedException("SQLite has no native vector type.");
        public string CosineDistanceOperator => throw new NotSupportedException("SQLite has no native vector type.");
        public string L1DistanceOperator => throw new NotSupportedException("SQLite has no native vector type.");
    }

    private sealed class SqliteFullTextDialect : IFullTextDialect
    {
        // SQLite full-text search uses the FTS5 module via MATCH queries on virtual tables.
        public string MatchOperator => "MATCH";
        public string ToTsVectorFunction => throw new NotSupportedException("SQLite uses FTS5 virtual tables instead of tsvector.");
        public string ToTsQueryFunction => throw new NotSupportedException("SQLite uses FTS5 MATCH instead of tsquery.");
        public string PlainToTsQueryFunction => throw new NotSupportedException("SQLite uses FTS5 MATCH instead of plainto_tsquery.");
        public string PhraseToTsQueryFunction => throw new NotSupportedException("SQLite uses FTS5 MATCH instead of phraseto_tsquery.");
        public string WebSearchToTsQueryFunction => throw new NotSupportedException("SQLite uses FTS5 MATCH instead of websearch_to_tsquery.");
        public string TsRankFunction => "rank";
        public string TsRankCoverDensityFunction => throw new NotSupportedException("SQLite has no equivalent to ts_rank_cd.");
        public string TsHeadlineFunction => "snippet";
    }

    private sealed class UnsupportedRangeDialect : IRangeDialect
    {
        public string ContainsElementOperator => throw new NotSupportedException("SQLite has no range type.");
        public string ContainedByOperator => throw new NotSupportedException("SQLite has no range type.");
        public string OverlapOperator => throw new NotSupportedException("SQLite has no range type.");
        public string AdjacentOperator => throw new NotSupportedException("SQLite has no range type.");
    }
}
