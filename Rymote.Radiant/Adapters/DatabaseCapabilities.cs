namespace Rymote.Radiant.Adapters;

[System.Flags]
public enum DatabaseCapabilities : long
{
    None = 0,
    ReturningClause = 1L << 0,
    UpsertOnConflict = 1L << 1,
    UpsertMerge = 1L << 2,
    CommonTableExpression = 1L << 3,
    RecursiveCommonTableExpression = 1L << 4,
    LateralJoin = 1L << 5,
    WindowFunctions = 1L << 6,
    SchemaPerTable = 1L << 7,
    JsonbColumn = 1L << 8,
    ArrayColumn = 1L << 9,
    VectorColumn = 1L << 10,
    FullTextSearch = 1L << 11,
    RangeTypes = 1L << 12,
    CaseInsensitiveText = 1L << 13,
    SpatialTypes = 1L << 14,
    CaseInsensitiveLikeOperator = 1L << 15,
    RegularExpressionOperator = 1L << 16,
    NamedSequences = 1L << 17,
    BatchedInsertReturning = 1L << 18,
}
