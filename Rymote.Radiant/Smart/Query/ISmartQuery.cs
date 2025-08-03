using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Builder;

namespace Rymote.Radiant.Smart.Query;

public interface ISmartQuery<TModel> where TModel : class, new()
{
    ISmartQuery<TModel> Where(Expression<Func<TModel, bool>> predicate);
    ISmartQuery<TModel> Where(string columnName, string operatorSymbol, object value);
    ISmartQuery<TModel> WhereRaw(string rawSql, params object[] parameters);
    ISmartQuery<TModel> WhereExists(IQueryBuilder subquery);
    ISmartQuery<TModel> WhereNotExists(IQueryBuilder subquery);
    ISmartQuery<TModel> WhereIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery);
    ISmartQuery<TModel> WhereNotIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery);
    
    ISmartQuery<TModel> OrWhere(Expression<Func<TModel, bool>> predicate);
    ISmartQuery<TModel> OrWhere(string columnName, string operatorSymbol, object value);
    
    ISmartQuery<TModel> OrderBy(Expression<Func<TModel, object>> keySelector);
    ISmartQuery<TModel> OrderByDescending(Expression<Func<TModel, object>> keySelector);
    ISmartQuery<TModel> OrderByRaw(string rawSql);
    
    ISmartQuery<TModel> Skip(int count);
    ISmartQuery<TModel> Take(int count);
    
    ISmartQuery<TModel> Include(Expression<Func<TModel, object>> navigationProperty);
    ISmartQuery<TModel> WithTrashed();
    ISmartQuery<TModel> OnlyTrashed();
    
    ISmartQuery<TModel> Join<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new();
    ISmartQuery<TModel> LeftJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new();
    ISmartQuery<TModel> RightJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new();
    
    ISmartQuery<TModel> GroupBy(params Expression<Func<TModel, object>>[] keySelectors);
    ISmartQuery<TModel> Having(string aggregateExpression, string operatorSymbol, object value);
    
    ISmartQuery<TModel> SelectDistinct();
    
    ISmartQuery<TModel> WhereJsonContains(Expression<Func<TModel, object>> property, string jsonPath, object value);
    ISmartQuery<TModel> WhereJsonExists(Expression<Func<TModel, object>> property, string jsonPath);
    
    ISmartQuery<TModel> WhereJsonbContains(Expression<Func<TModel, object>> property, object jsonObject);
    ISmartQuery<TModel> WhereJsonbPathExists(Expression<Func<TModel, object>> property, string jsonPath);
    ISmartQuery<TModel> WhereJsonbHasKey(Expression<Func<TModel, object>> property, string key);
    ISmartQuery<TModel> WhereJsonbHasAnyKeys(Expression<Func<TModel, object>> property, params string[] keys);
    ISmartQuery<TModel> WhereJsonbHasAllKeys(Expression<Func<TModel, object>> property, params string[] keys);
    
    ISmartQuery<TModel> WhereArrayContains<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements);
    ISmartQuery<TModel> WhereArrayContainedBy<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements);
    ISmartQuery<TModel> WhereArrayOverlaps<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements);
    
    ISmartQuery<TModel> WhereFullTextSearch(Expression<Func<TModel, object>> property, string searchQuery, string language = "english");
    ISmartQuery<TModel> OrderByFullTextRank(Expression<Func<TModel, object>> property, string searchQuery, string language = "english");
    
    ISmartQuery<TModel> WhereVectorSimilarity(Expression<Func<TModel, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance, float threshold = 0.5f);
    ISmartQuery<TModel> OrderByVectorDistance(Expression<Func<TModel, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance);
    
    ISmartQuery<TModel> With(string cteName, IQueryBuilder cteQuery);
    ISmartQuery<TModel> WithRecursive(string cteName, IQueryBuilder cteQuery);
    
    Task<TModel?> FirstOrDefaultAsync();
    Task<TModel> FirstAsync();
    Task<List<TModel>> ToListAsync();
    Task<int> CountAsync();
    Task<bool> AnyAsync();
    
    Task<TResult?> MaxAsync<TResult>(Expression<Func<TModel, TResult>> selector);
    Task<TResult?> MinAsync<TResult>(Expression<Func<TModel, TResult>> selector);
    Task<decimal> SumAsync(Expression<Func<TModel, decimal>> selector);
    Task<double> AverageAsync(Expression<Func<TModel, double>> selector);
    
    Task<List<TResult>> ToListAsync<TResult>(Expression<Func<TModel, TResult>> selector) where TResult : class;
    Task<Dictionary<TKey, List<TModel>>> GroupByAsync<TKey>(Expression<Func<TModel, TKey>> keySelector) where TKey : notnull;
}