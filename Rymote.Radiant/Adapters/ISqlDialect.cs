namespace Rymote.Radiant.Adapters;

public interface ISqlDialect
{
    string Select { get; }
    string From { get; }
    string Where { get; }
    string GroupBy { get; }
    string Having { get; }
    string OrderBy { get; }
    string Ascending { get; }
    string Descending { get; }
    string Limit { get; }
    string Offset { get; }
    string InsertInto { get; }
    string Update { get; }
    string Delete { get; }
    string Set { get; }
    string Values { get; }
    string ReturningKeyword { get; }
    string OnConflict { get; }
    string DoNothing { get; }
    string DoUpdate { get; }
    string ExcludedTableAlias { get; }
    string CaseInsensitiveLikeOperator { get; }
    string CurrentTimestampExpression { get; }
    string CastOperator { get; }
    string ConcatenateOperator { get; }
    string With { get; }
    string Recursive { get; }
    string LateralKeyword { get; }
    string NullLiteral { get; }
    string TrueLiteral { get; }
    string FalseLiteral { get; }

    IJsonbDialect Jsonb { get; }
    IArrayDialect Array { get; }
    IVectorDialect Vector { get; }
    IFullTextDialect FullText { get; }
    IRangeDialect Range { get; }
}

public interface IJsonbDialect
{
    string ExtractText { get; }
    string ExtractJson { get; }
    string ContainsOperator { get; }
    string ContainedByOperator { get; }
    string HasKeyOperator { get; }
    string HasAnyKeyOperator { get; }
    string HasAllKeysOperator { get; }
    string PathExistsOperator { get; }
    string PathMatchOperator { get; }
    string ConcatenateOperator { get; }
    string DeletePathOperator { get; }
}

public interface IArrayDialect
{
    string ContainsOperator { get; }
    string ContainedByOperator { get; }
    string OverlapOperator { get; }
    string ConcatenateOperator { get; }
    string CardinalityFunction { get; }
    string UnnestFunction { get; }
}

public interface IVectorDialect
{
    string L2DistanceOperator { get; }
    string InnerProductOperator { get; }
    string CosineDistanceOperator { get; }
    string L1DistanceOperator { get; }
}

public interface IFullTextDialect
{
    string MatchOperator { get; }
    string ToTsVectorFunction { get; }
    string ToTsQueryFunction { get; }
    string PlainToTsQueryFunction { get; }
    string PhraseToTsQueryFunction { get; }
    string WebSearchToTsQueryFunction { get; }
    string TsRankFunction { get; }
    string TsRankCoverDensityFunction { get; }
    string TsHeadlineFunction { get; }
}

public interface IRangeDialect
{
    string ContainsElementOperator { get; }
    string ContainedByOperator { get; }
    string OverlapOperator { get; }
    string AdjacentOperator { get; }
}
