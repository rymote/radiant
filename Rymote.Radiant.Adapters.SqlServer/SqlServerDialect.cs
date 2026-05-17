using System;
using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Adapters.SqlServer;

public sealed class SqlServerDialect : ISqlDialect
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
    // T-SQL emits OUTPUT INSERTED.col before the VALUES list, not after; ReturningClause emission ordering differs from PostgreSQL.
    public string ReturningKeyword => "OUTPUT";
    public string OnConflict => throw new NotSupportedException("SQL Server uses MERGE statements instead of ON CONFLICT.");
    public string DoNothing => throw new NotSupportedException("SQL Server uses MERGE statements instead of ON CONFLICT.");
    public string DoUpdate => throw new NotSupportedException("SQL Server uses MERGE statements instead of ON CONFLICT.");
    public string ExcludedTableAlias => throw new NotSupportedException("SQL Server uses MERGE statements instead of ON CONFLICT.");
    // SQL Server LIKE is collation-dependent for case sensitivity; a CI collation makes LIKE case-insensitive.
    public string CaseInsensitiveLikeOperator => "LIKE";
    public string CurrentTimestampExpression => "CURRENT_TIMESTAMP";
    public string CastOperator => throw new NotSupportedException("SQL Server uses CAST(value AS type) function syntax instead of the :: cast operator.");
    public string ConcatenateOperator => "+";
    public string With => "WITH";
    public string Recursive => "RECURSIVE";
    public string LateralKeyword => throw new NotSupportedException("SQL Server uses CROSS APPLY / OUTER APPLY rather than the LATERAL keyword.");
    public string NullLiteral => "NULL";
    public string TrueLiteral => "1";
    public string FalseLiteral => "0";

    public IJsonbDialect Jsonb { get; } = new UnsupportedJsonbDialect();
    public IArrayDialect Array { get; } = new UnsupportedArrayDialect();
    public IVectorDialect Vector { get; } = new UnsupportedVectorDialect();
    public IFullTextDialect FullText { get; } = new UnsupportedFullTextDialect();
    public IRangeDialect Range { get; } = new UnsupportedRangeDialect();

    private sealed class UnsupportedJsonbDialect : IJsonbDialect
    {
        public string ExtractText => throw new NotSupportedException("SQL Server has no JSONB type; use JSON_VALUE / JSON_QUERY functions instead.");
        public string ExtractJson => throw new NotSupportedException("SQL Server has no JSONB type; use JSON_QUERY instead.");
        public string ContainsOperator => throw new NotSupportedException("SQL Server has no JSON contains operator.");
        public string ContainedByOperator => throw new NotSupportedException("SQL Server has no JSON contained-by operator.");
        public string HasKeyOperator => throw new NotSupportedException("SQL Server has no JSON 'has key' operator; use JSON_VALUE IS NOT NULL.");
        public string HasAnyKeyOperator => throw new NotSupportedException("SQL Server has no JSON 'has any key' operator.");
        public string HasAllKeysOperator => throw new NotSupportedException("SQL Server has no JSON 'has all keys' operator.");
        public string PathExistsOperator => throw new NotSupportedException("SQL Server has no JSON 'path exists' operator.");
        public string PathMatchOperator => throw new NotSupportedException("SQL Server has no JSON 'path match' operator.");
        public string ConcatenateOperator => throw new NotSupportedException("SQL Server has no JSON concatenation operator; use JSON_MODIFY.");
        public string DeletePathOperator => throw new NotSupportedException("SQL Server has no JSON 'delete path' operator; use JSON_MODIFY with NULL.");
    }

    private sealed class UnsupportedArrayDialect : IArrayDialect
    {
        public string ContainsOperator => throw new NotSupportedException("SQL Server has no array column type.");
        public string ContainedByOperator => throw new NotSupportedException("SQL Server has no array column type.");
        public string OverlapOperator => throw new NotSupportedException("SQL Server has no array column type.");
        public string ConcatenateOperator => throw new NotSupportedException("SQL Server has no array column type.");
        public string CardinalityFunction => throw new NotSupportedException("SQL Server has no array column type.");
        public string UnnestFunction => throw new NotSupportedException("SQL Server has no array column type.");
    }

    private sealed class UnsupportedVectorDialect : IVectorDialect
    {
        public string L2DistanceOperator => throw new NotSupportedException("SQL Server has no native vector type.");
        public string InnerProductOperator => throw new NotSupportedException("SQL Server has no native vector type.");
        public string CosineDistanceOperator => throw new NotSupportedException("SQL Server has no native vector type.");
        public string L1DistanceOperator => throw new NotSupportedException("SQL Server has no native vector type.");
    }

    private sealed class UnsupportedFullTextDialect : IFullTextDialect
    {
        // SQL Server full-text search uses CONTAINS / FREETEXT predicates with very different syntax; the operator/function shape here does not map.
        public string MatchOperator => throw new NotSupportedException("SQL Server uses CONTAINS / FREETEXT predicates instead of a match operator.");
        public string ToTsVectorFunction => throw new NotSupportedException("SQL Server has no to_tsvector equivalent; full-text indexes are configured at the table level.");
        public string ToTsQueryFunction => throw new NotSupportedException("SQL Server has no to_tsquery equivalent.");
        public string PlainToTsQueryFunction => throw new NotSupportedException("SQL Server has no plainto_tsquery equivalent.");
        public string PhraseToTsQueryFunction => throw new NotSupportedException("SQL Server has no phraseto_tsquery equivalent.");
        public string WebSearchToTsQueryFunction => throw new NotSupportedException("SQL Server has no websearch_to_tsquery equivalent.");
        public string TsRankFunction => throw new NotSupportedException("SQL Server has no ts_rank equivalent; use the rank column returned by CONTAINSTABLE.");
        public string TsRankCoverDensityFunction => throw new NotSupportedException("SQL Server has no ts_rank_cd equivalent.");
        public string TsHeadlineFunction => throw new NotSupportedException("SQL Server has no ts_headline equivalent.");
    }

    private sealed class UnsupportedRangeDialect : IRangeDialect
    {
        public string ContainsElementOperator => throw new NotSupportedException("SQL Server has no range type.");
        public string ContainedByOperator => throw new NotSupportedException("SQL Server has no range type.");
        public string OverlapOperator => throw new NotSupportedException("SQL Server has no range type.");
        public string AdjacentOperator => throw new NotSupportedException("SQL Server has no range type.");
    }
}
