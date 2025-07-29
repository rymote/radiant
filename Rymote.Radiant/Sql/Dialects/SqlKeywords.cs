namespace Rymote.Radiant.Sql.Dialects;

public static class SqlKeywords
{
    // Basic SQL Commands
    public const string SELECT = "SELECT";
    public const string INSERT_INTO = "INSERT INTO";
    public const string UPDATE = "UPDATE";
    public const string DELETE = "DELETE";
    public const string FROM = "FROM";
    public const string WHERE = "WHERE";
    public const string SET = "SET";
    public const string VALUES = "VALUES";
    public const string ARRAY = "ARRAY";

    public const string DISTINCT = "DISTINCT";
    public const string AS = "AS";
    public const string NULL = "NULL";

    // Joins
    public const string INNER_JOIN = "INNER JOIN";
    public const string LEFT_JOIN = "LEFT JOIN";
    public const string RIGHT_JOIN = "RIGHT JOIN";
    public const string FULL_JOIN = "FULL JOIN";
    public const string CROSS_JOIN = "CROSS JOIN";
    public const string ON = "ON";

    // Logical Operators
    public const string AND = "AND";
    public const string OR = "OR";
    public const string NOT = "NOT";
    public const string EXISTS = "EXISTS";
    public const string NOT_EXISTS = "NOT EXISTS";

    // Sorting and Grouping
    public const string ORDER_BY = "ORDER BY";
    public const string GROUP_BY = "GROUP BY";
    public const string HAVING = "HAVING";
    public const string ASC = "ASC";
    public const string DESC = "DESC";

    // Limits and Pagination
    public const string LIMIT = "LIMIT";
    public const string OFFSET = "OFFSET";

    // Set Operations
    public const string UNION = "UNION";
    public const string UNION_ALL = "UNION ALL";
    public const string INTERSECT = "INTERSECT";
    public const string EXCEPT = "EXCEPT";

    // Common Table Expressions (CTEs)
    public const string WITH = "WITH";
    public const string RECURSIVE = "RECURSIVE";

    // Case Expressions
    public const string CASE = "CASE";
    public const string WHEN = "WHEN";
    public const string THEN = "THEN";
    public const string ELSE = "ELSE";
    public const string END = "END";

    // Window Functions
    public const string OVER = "OVER";
    public const string PARTITION_BY = "PARTITION BY";
    public const string ROWS = "ROWS";
    public const string RANGE = "RANGE";
    public const string BETWEEN = "BETWEEN";
    public const string UNBOUNDED_PRECEDING = "UNBOUNDED PRECEDING";
    public const string PRECEDING = "PRECEDING";
    public const string CURRENT_ROW = "CURRENT ROW";
    public const string FOLLOWING = "FOLLOWING";
    public const string UNBOUNDED_FOLLOWING = "UNBOUNDED FOLLOWING";

    // Functions and Expressions
    public const string EXTRACT = "EXTRACT";
    public const string CAST = "CAST";

    // PostgreSQL specific
    public const string RETURNING = "RETURNING";

    // Date/Time Functions
    public const string CURRENT_DATE = "CURRENT_DATE";
    public const string CURRENT_TIME = "CURRENT_TIME";
    public const string CURRENT_TIMESTAMP = "CURRENT_TIMESTAMP";
    public const string NOW_FUNCTION = "NOW()";
    public const string DATE_TRUNC = "date_trunc";

    // Boolean Literals
    public const string TRUE = "TRUE";
    public const string FALSE = "FALSE";

    // Common SQL Functions
    public const string COALESCE = "COALESCE";
    public const string NULLIF = "NULLIF";
    public const string ROUND = "ROUND";
    public const string TO_CHAR = "TO_CHAR";
    public const string CONCAT = "CONCAT";
    public const string UPPER = "UPPER";
    public const string LOWER = "LOWER";

    // Separators and Operators
    public const string DOT = ".";
    public const string COMMA = ", ";
    public const string SPACE = " ";
    public const string EQUALS = " = ";
    public const string NOT_EQUALS = " != ";
    public const string OPEN_PAREN = "(";
    public const string CLOSE_PAREN = ")";
    public const string OPEN_BRACKET = "[";
    public const string CLOSE_BRACKET = "]";
    public const string QUOTE = "\"";
    public const string SINGLE_QUOTE = "'";
    public const string PARAMETER_PREFIX = "@";

    // PostgreSQL Cast Operator
    public const string CAST_OPERATOR = "::";

    // Math Operators
    public const string PLUS = "+";
    public const string MINUS = "-";
    public const string MULTIPLY = "*";
    public const string DIVIDE = "/";
    public const string MODULO = "%";

    // JSON/JSONB Operators (PostgreSQL)
    public const string JSON_EXTRACT_TEXT = "->>";
    public const string JSON_EXTRACT_JSON = "->";
    public const string JSON_PATH_EXISTS = "?";
    public const string JSON_CONTAINS_KEY = "?&";
    public const string JSON_CONTAINS = "@>";
    public const string JSON_CONTAINED_BY = "<@";

    // ===== NEW: Advanced PostgreSQL Features =====

    // UPSERT & Conflict Resolution
    public const string ON_CONFLICT = "ON CONFLICT";
    public const string ON_CONSTRAINT = "ON CONSTRAINT";
    public const string DO_NOTHING = "DO NOTHING";
    public const string DO_UPDATE = "DO UPDATE";
    public const string EXCLUDED = "EXCLUDED";

    // Vector Search Operators (pgvector)
    public const string VECTOR_L2_DISTANCE = "<->"; // L2 distance
    public const string VECTOR_INNER_PRODUCT = "<#>"; // Inner product (negative)
    public const string VECTOR_COSINE_DISTANCE = "<=>"; // Cosine distance
    public const string VECTOR_L1_DISTANCE = "<+>"; // L1/Manhattan distance (newer pgvector)

    // Advanced JSONB Operators
    public const string JSONB_PATH_EXISTS = "@?"; // JSONPath exists
    public const string JSONB_PATH_MATCH = "@@"; // JSONPath match
    public const string JSONB_DELETE_PATH = "#-"; // Delete path
    public const string JSONB_CONCAT = "||"; // Concatenate
    public const string JSONB_HAS_KEY = "?"; // Has key (same as JSON)
    public const string JSONB_HAS_ANY_KEY = "?|"; // Has any key
    public const string JSONB_HAS_ALL_KEYS = "?&"; // Has all keys
    public const string JSONB_STRICT_CONTAINS = "@>"; // Strict containment
    public const string JSONB_STRICT_CONTAINED = "<@"; // Strict contained by

    // Full-Text Search
    public const string FTS_MATCH = "@@"; // Text search match
    public const string FTS_TO_TSVECTOR = "to_tsvector";
    public const string FTS_TO_TSQUERY = "to_tsquery";
    public const string FTS_PLAINTO_TSQUERY = "plainto_tsquery";
    public const string FTS_PHRASETO_TSQUERY = "phraseto_tsquery";
    public const string FTS_WEBSEARCH_TO_TSQUERY = "websearch_to_tsquery";
    public const string FTS_TS_RANK = "ts_rank";
    public const string FTS_TS_RANK_CD = "ts_rank_cd";
    public const string FTS_TS_HEADLINE = "ts_headline";

    // Vector Functions
    public const string VECTOR_L2_NORMALIZE = "l2_normalize";
    public const string VECTOR_INNER_PRODUCT_FUNC = "inner_product";
    public const string VECTOR_COSINE_SIMILARITY = "cosine_similarity";
    public const string VECTOR_L1_DISTANCE_FUNC = "l1_distance";
    public const string VECTOR_L2_DISTANCE_FUNC = "l2_distance";

    // PostGIS Spatial (for future implementation)
    public const string ST_WITHIN = "ST_Within";
    public const string ST_CONTAINS = "ST_Contains";
    public const string ST_INTERSECTS = "ST_Intersects";
    public const string ST_DISTANCE = "ST_Distance";
    public const string ST_DWITHIN = "ST_DWithin";
    public const string ST_OVERLAPS = "ST_Overlaps";
    public const string ST_TOUCHES = "ST_Touches";
    public const string ST_CROSSES = "ST_Crosses";
    public const string ST_ASTEXT = "ST_AsText";
    public const string ST_ASBINARY = "ST_AsBinary";
    public const string ST_GEOMFROMTEXT = "ST_GeomFromText";
    public const string ST_MAKEPOINT = "ST_MakePoint";

    // Spatial Operators (PostGIS)
    public const string SPATIAL_OVERLAPS = "&&"; // Overlaps
    public const string SPATIAL_LEFT = "<<"; // Is left of
    public const string SPATIAL_RIGHT = ">>"; // Is right of
    public const string SPATIAL_BELOW = "<<|"; // Is below
    public const string SPATIAL_ABOVE = "|>>"; // Is above
    public const string SPATIAL_OVERLAPS_OR_LEFT = "&<"; // Overlaps or is left of
    public const string SPATIAL_OVERLAPS_OR_RIGHT = "&>"; // Overlaps or is right of
    public const string SPATIAL_OVERLAPS_OR_BELOW = "&<|"; // Overlaps or is below
    public const string SPATIAL_OVERLAPS_OR_ABOVE = "|&>"; // Overlaps or is above

    // Array Operators (PostgreSQL)
    public const string ARRAY_CONTAINS = "@>"; // Array contains
    public const string ARRAY_CONTAINED_BY = "<@"; // Array contained by
    public const string ARRAY_OVERLAP = "&&"; // Array overlap
    public const string ARRAY_CONCAT = "||"; // Array concatenation
    
    // Array Functions
    public const string ARRAY_LENGTH = "array_length";
    public const string ARRAY_POSITION = "array_position";
    public const string ARRAY_POSITIONS = "array_positions";
    public const string ARRAY_APPEND = "array_append";
    public const string ARRAY_PREPEND = "array_prepend";
    public const string ARRAY_REMOVE = "array_remove";
    public const string ARRAY_REPLACE = "array_replace";
    public const string ARRAY_TO_STRING = "array_to_string";
    public const string STRING_TO_ARRAY = "string_to_array";
    public const string ARRAY_DIMS = "array_dims";
    public const string ARRAY_LOWER = "array_lower";
    public const string ARRAY_UPPER = "array_upper";
    public const string CARDINALITY = "cardinality";

    // Range Types (PostgreSQL)
    public const string RANGE_CONTAINS_ELEMENT = "@>"; // Range contains element
    public const string RANGE_CONTAINED_BY = "<@"; // Range contained by
    public const string RANGE_OVERLAP = "&&"; // Range overlap
    public const string RANGE_LEFT_OF = "<<"; // Range left of
    public const string RANGE_RIGHT_OF = ">>"; // Range right of
    public const string RANGE_ADJACENT = "-|-"; // Range adjacent
    public const string RANGE_UNION = "+"; // Range union
    public const string RANGE_INTERSECTION = "*"; // Range intersection
    public const string RANGE_DIFFERENCE = "-"; // Range difference

    // Network Address Types (INET/CIDR)
    public const string INET_CONTAINS = ">>="; // Network contains
    public const string INET_CONTAINED_BY = "<<="; // Network contained by
    public const string INET_OVERLAP = "&&"; // Network overlap

    // Additional PostgreSQL Functions
    public const string GENERATE_SERIES = "generate_series";
    public const string UNNEST = "unnest";
    public const string ARRAY_AGG = "array_agg";
    public const string STRING_AGG = "string_agg";
    public const string JSON_AGG = "json_agg";
    public const string JSONB_AGG = "jsonb_agg";
    public const string JSON_OBJECT_AGG = "json_object_agg";
    public const string JSONB_OBJECT_AGG = "jsonb_object_agg";

    // PostgreSQL String Functions
    public const string REGEXP_MATCH = "regexp_match";
    public const string REGEXP_MATCHES = "regexp_matches";
    public const string REGEXP_REPLACE = "regexp_replace";
    public const string REGEXP_SPLIT_TO_ARRAY = "regexp_split_to_array";
    public const string REGEXP_SPLIT_TO_TABLE = "regexp_split_to_table";
    public const string SIMILARITY = "similarity"; // pg_trgm extension
    public const string WORD_SIMILARITY = "word_similarity"; // pg_trgm extension

    // Pattern Matching Operators
    public const string LIKE = "LIKE";
    public const string ILIKE = "ILIKE"; // Case-insensitive LIKE
    public const string SIMILAR_TO = "SIMILAR TO";
    public const string REGEXP_MATCH_OP = "~"; // Regular expression match
    public const string REGEXP_IMATCH_OP = "~*"; // Case-insensitive regex match
    public const string NOT_REGEXP_MATCH = "!~"; // Does not match regex
    public const string NOT_REGEXP_IMATCH = "!~*"; // Case-insensitive not match

    // PostgreSQL-specific Keywords
    public const string LATERAL = "LATERAL";
    public const string TABLESAMPLE = "TABLESAMPLE";
    public const string ORDINALITY = "ORDINALITY";
    public const string ONLY = "ONLY"; // For inheritance
    public const string INHERITS = "INHERITS";

    // Index Types (for CREATE INDEX - future feature)
    public const string USING_BTREE = "USING btree";
    public const string USING_HASH = "USING hash";
    public const string USING_GIN = "USING gin";
    public const string USING_GIST = "USING gist";
    public const string USING_SPGIST = "USING spgist";
    public const string USING_BRIN = "USING brin";

    // Constraint Types
    public const string PRIMARY_KEY = "PRIMARY KEY";
    public const string FOREIGN_KEY = "FOREIGN KEY";
    public const string UNIQUE = "UNIQUE";
    public const string CHECK = "CHECK";
    public const string NOT_NULL = "NOT NULL";
    public const string DEFAULT = "DEFAULT";

    // Advanced SQL Keywords
    public const string FILTER = "FILTER"; // For aggregate functions
    public const string WITHIN_GROUP = "WITHIN GROUP"; // For ordered-set aggregates
    public const string RESPECT_NULLS = "RESPECT NULLS";
    public const string IGNORE_NULLS = "IGNORE NULLS";
}