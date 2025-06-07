using Rymote.Radiant.Query;
using System.Linq;

namespace Rymote.Radiant.Extensions;

public static class QueryExtensions
{
    public static QueryBuilder<T> WhereDate<T>(this QueryBuilder<T> query, string column, DateTime date) where T : class
    {
        return query.Where($"DATE({column})", "=", date.Date);
    }

    public static QueryBuilder<T> WhereYear<T>(this QueryBuilder<T> query, string column, int year) where T : class
    {
        return query.Where($"EXTRACT(YEAR FROM {column})", "=", year);
    }

    public static QueryBuilder<T> WhereMonth<T>(this QueryBuilder<T> query, string column, int month) where T : class
    {
        return query.Where($"EXTRACT(MONTH FROM {column})", "=", month);
    }

    public static QueryBuilder<T> WhereDay<T>(this QueryBuilder<T> query, string column, int day) where T : class
    {
        return query.Where($"EXTRACT(DAY FROM {column})", "=", day);
    }

    public static QueryBuilder<T> WhereFullText<T>(this QueryBuilder<T> query, string column, string searchTerm, string language = "english") where T : class
    {
        string paramName = $"search_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, searchTerm }
        };
        return query.Where($"to_tsvector('{language}', {column}) @@ plainto_tsquery('{language}', @{paramName})", parameters);
    }

    public static QueryBuilder<T> WithinDistance<T>(this QueryBuilder<T> query, string latColumn, string lonColumn, double latitude, double longitude, double radiusKm) where T : class
    {
        string latParam = $"lat_{Guid.NewGuid():N}";
        string lonParam = $"lon_{Guid.NewGuid():N}";
        string radiusParam = $"radius_{Guid.NewGuid():N}";
        
        var parameters = new Dictionary<string, object>
        {
            { latParam, latitude },
            { lonParam, longitude },
            { radiusParam, radiusKm * 1000 }
        };
        
        return query.Where($"ST_DWithin(ST_Point({lonColumn}, {latColumn})::geography, ST_Point(@{lonParam}, @{latParam})::geography, @{radiusParam})", parameters);
    }

    public static QueryBuilder<T> OrderByDistance<T>(this QueryBuilder<T> query, string latColumn, string lonColumn, double latitude, double longitude) where T : class
    {
        string latParam = $"lat_{Guid.NewGuid():N}";
        string lonParam = $"lon_{Guid.NewGuid():N}";
        
        var parameters = new Dictionary<string, object>
        {
            { latParam, latitude },
            { lonParam, longitude }
        };
        
        return query.OrderByRaw($"ST_Distance(ST_Point({lonColumn}, {latColumn})::geography, ST_Point(@{lonParam}, @{latParam})::geography)")
                   .Where("1 = 1", parameters);
    }

    // Case-insensitive search (from DealsQuery pattern)
    public static QueryBuilder<T> WhereILike<T>(this QueryBuilder<T> query, string column, string pattern) where T : class
    {
        string paramName = $"pattern_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, pattern }
        };
        return query.Where($"LOWER({column}) LIKE LOWER(@{paramName})", parameters);
    }

    // JSON operations (PostgreSQL)
    public static QueryBuilder<T> WhereJsonContains<T>(this QueryBuilder<T> query, string column, string jsonPath, object value) where T : class
    {
        string paramName = $"json_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, value }
        };
        return query.Where($"{column}->>'{jsonPath}' = @{paramName}", parameters);
    }

    public static QueryBuilder<T> WhereJsonArrayContains<T>(this QueryBuilder<T> query, string column, object value) where T : class
    {
        string paramName = $"jsonarray_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, value }
        };
        return query.Where($"{column} @> @{paramName}::jsonb", parameters);
    }

    // Array operations (PostgreSQL)
    public static QueryBuilder<T> WhereInArray<T>(this QueryBuilder<T> query, string column, object value) where T : class
    {
        string paramName = $"array_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, value }
        };
        return query.Where($"@{paramName} = ANY({column})", parameters);
    }

    public static QueryBuilder<T> WhereArrayContains<T>(this QueryBuilder<T> query, string column, params object[] values) where T : class
    {
        string paramName = $"arraycontains_{Guid.NewGuid():N}";
        var parameters = new Dictionary<string, object>
        {
            { paramName, values }
        };
        return query.Where($"{column} @> @{paramName}", parameters);
    }

    // Date range operations (common in DealsQuery)
    public static QueryBuilder<T> WhereBetweenDates<T>(this QueryBuilder<T> query, string column, DateTime? startDate, DateTime? endDate) where T : class
    {
        if (startDate.HasValue)
            query = query.Where(column, ">=", startDate.Value);
        if (endDate.HasValue)
            query = query.Where(column, "<=", endDate.Value);
        return query;
    }

    public static QueryBuilder<T> WhereInLastDays<T>(this QueryBuilder<T> query, string column, int days) where T : class
    {
        return query.Where(column, ">=", DateTime.UtcNow.AddDays(-days));
    }

    public static QueryBuilder<T> WhereInCurrentMonth<T>(this QueryBuilder<T> query, string column) where T : class
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        return query.WhereBetweenDates(column, startOfMonth, endOfMonth);
    }

    // Null handling (common pattern)
    public static QueryBuilder<T> WhereNullOrEmpty<T>(this QueryBuilder<T> query, string column) where T : class
    {
        return query.Where($"({column} IS NULL OR {column} = '')", new Dictionary<string, object>());
    }

    public static QueryBuilder<T> WhereNotNullOrEmpty<T>(this QueryBuilder<T> query, string column) where T : class
    {
        return query.Where($"({column} IS NOT NULL AND {column} != '')", new Dictionary<string, object>());
    }

    // Aggregation helpers
    public static QueryBuilder<T> WithCount<T>(this QueryBuilder<T> query, string alias = "total_count") where T : class
    {
        return query.SelectRaw($"COUNT(*) OVER() as {alias}");
    }

    public static QueryBuilder<T> WithRowNumber<T>(this QueryBuilder<T> query, string orderByColumn, string alias = "row_num") where T : class
    {
        return query.SelectRaw($"ROW_NUMBER() OVER (ORDER BY {orderByColumn}) as {alias}");
    }

    // Subquery patterns (from DealsQuery)
    public static QueryBuilder<T> WhereExists<T>(this QueryBuilder<T> query, string subquery, Dictionary<string, object>? parameters = null) where T : class
    {
        return query.Where($"EXISTS ({subquery})", parameters ?? new Dictionary<string, object>());
    }

    public static QueryBuilder<T> WhereNotExists<T>(this QueryBuilder<T> query, string subquery, Dictionary<string, object>? parameters = null) where T : class
    {
        return query.Where($"NOT EXISTS ({subquery})", parameters ?? new Dictionary<string, object>());
    }

    // Multi-column search (common in DealsQuery)
    public static QueryBuilder<T> WhereAnyColumn<T>(this QueryBuilder<T> query, string[] columns, string @operator, object value) where T : class
    {
        var conditions = columns.Select(col => $"{col} {@operator} @value").ToArray();
        var condition = $"({string.Join(" OR ", conditions)})";
        return query.Where(condition, new Dictionary<string, object> { { "value", value } });
    }

    public static QueryBuilder<T> SearchAcrossColumns<T>(this QueryBuilder<T> query, string[] columns, string searchTerm) where T : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var paramName = $"search_{Guid.NewGuid():N}";
        var searchPattern = $"%{searchTerm}%";
        var conditions = columns.Select(col => $"LOWER({col}) LIKE LOWER(@{paramName})").ToArray();
        var condition = $"({string.Join(" OR ", conditions)})";
        
        return query.Where(condition, new Dictionary<string, object> { { paramName, searchPattern } });
    }

    // Enum/Status helpers (from DealsQuery status filtering)
    public static QueryBuilder<T> WhereEnum<T, TEnum>(this QueryBuilder<T> query, string column, TEnum enumValue) where T : class where TEnum : Enum
    {
        return query.Where(column, "=", Convert.ToInt32(enumValue));
    }

    public static QueryBuilder<T> WhereInEnum<T, TEnum>(this QueryBuilder<T> query, string column, params TEnum[] enumValues) where T : class where TEnum : Enum
    {
        var intValues = enumValues.Select(e => Convert.ToInt32(e)).ToArray();
        return query.WhereIn(column, intValues);
    }

    // Pagination with total count (common pattern)
    public static async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync<T>(this QueryBuilder<T> query, int page, int pageSize) where T : class
    {
        // Clone the query for count
        var countQuery = query.Clone();
        var totalCount = await countQuery.CountAsync();
        
        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .GetAsync();
        
        return (items, totalCount);
    }

    // Conditional query building (from DealsQuery pattern)
    public static QueryBuilder<T> WhereIf<T>(this QueryBuilder<T> query, bool condition, string column, string @operator, object value) where T : class
    {
        return condition ? query.Where(column, @operator, value) : query;
    }

    public static QueryBuilder<T> WhereIf<T>(this QueryBuilder<T> query, bool condition, Func<QueryBuilder<T>, QueryBuilder<T>> queryFunc) where T : class
    {
        return condition ? queryFunc(query) : query;
    }

    // Soft delete support (common pattern)
    public static QueryBuilder<T> ExcludeDeleted<T>(this QueryBuilder<T> query, string deletedAtColumn = "deleted_at") where T : class
    {
        return query.Where($"{deletedAtColumn} IS NULL", new Dictionary<string, object>());
    }

    public static QueryBuilder<T> OnlyDeleted<T>(this QueryBuilder<T> query, string deletedAtColumn = "deleted_at") where T : class
    {
        return query.Where($"{deletedAtColumn} IS NOT NULL", new Dictionary<string, object>());
    }
}
