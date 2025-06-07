namespace Rymote.Radiant.Query;

public class PaginatedQueryBuilder<TModel> : QueryBuilder<TModel> where TModel : class
{
    public PaginatedQueryBuilder(System.Data.IDbConnection connection, string tableName, string schema = "public") 
        : base(connection, tableName, schema)
    {
    }

    public async Task<PaginatedResult<TModel>> PaginateAsync(int page, int pageSize)
    {
        int totalCount = await CountAsync();
        
        Offset((page - 1) * pageSize);
        Limit(pageSize);
        
        IEnumerable<TModel> items = await GetAsync();
        
        return new PaginatedResult<TModel>
        {
            Items = items.ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            HasNextPage = page * pageSize < totalCount,
            HasPreviousPage = page > 1
        };
    }
}
