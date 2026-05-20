using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Builder;

namespace Rymote.Radiant.Smart.Query;

public interface ISmartQuery<TModel> where TModel : class, new()
{
    ISmartQuery<TModel> Schema(string schemaName);
    ISmartQuery<TModel> Where(Expression<Func<TModel, bool>> predicate);
    ISmartQuery<TModel> Where(string columnName, string operatorSymbol, object value);
    ISmartQuery<TModel> WhereRaw(string rawSql, params object[] parameters);
    ISmartQuery<TModel> WhereExists(IQueryBuilder subquery);
    ISmartQuery<TModel> WhereNotExists(IQueryBuilder subquery);
    ISmartQuery<TModel> WhereIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery);
    ISmartQuery<TModel> WhereNotIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery);

    /// <summary>
    /// Restricts the result set to rows whose <paramref name="property"/> value appears in
    /// <paramref name="values"/>. Emits a parameterised <c>IN (...)</c> clause with one
    /// placeholder per value. Each value is bound through the normal Dapper parameter
    /// pipeline, so Radiant value converters registered for <typeparamref name="TKey"/>
    /// apply element-wise. An empty <paramref name="values"/> sequence is treated as a
    /// match-nothing filter (emits <c>1 = 0</c>) so the query stays valid.
    /// </summary>
    ISmartQuery<TModel> WhereIn<TKey>(Expression<Func<TModel, TKey>> property, IEnumerable<TKey> values);

    /// <summary>
    /// The negation of <see cref="WhereIn{TKey}(Expression{Func{TModel, TKey}}, IEnumerable{TKey})"/>.
    /// An empty <paramref name="values"/> sequence is treated as a match-all filter.
    /// </summary>
    ISmartQuery<TModel> WhereNotIn<TKey>(Expression<Func<TModel, TKey>> property, IEnumerable<TKey> values);

    ISmartQuery<TModel> OrWhere(Expression<Func<TModel, bool>> predicate);
    ISmartQuery<TModel> OrWhere(string columnName, string operatorSymbol, object value);

    ISmartQuery<TModel> OrderBy(Expression<Func<TModel, object>> keySelector);
    ISmartQuery<TModel> OrderByDescending(Expression<Func<TModel, object>> keySelector);
    ISmartQuery<TModel> OrderByRaw(string rawSql);

    ISmartQuery<TModel> Skip(int count);
    ISmartQuery<TModel> Take(int count);

    ISmartQuery<TModel> Include(Expression<Func<TModel, object>> navigationProperty);
    ISmartQuery<TModel> Include(string navigationPath);
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

    Task<TModel?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<TModel> FirstAsync(CancellationToken cancellationToken = default);
    Task<List<TModel>> ToListAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<TResult?> MaxAsync<TResult>(Expression<Func<TModel, TResult>> selector, CancellationToken cancellationToken = default);
    Task<TResult?> MinAsync<TResult>(Expression<Func<TModel, TResult>> selector, CancellationToken cancellationToken = default);
    Task<decimal> SumAsync(Expression<Func<TModel, decimal>> selector, CancellationToken cancellationToken = default);
    Task<double> AverageAsync(Expression<Func<TModel, double>> selector, CancellationToken cancellationToken = default);

    Task<List<TResult>> ToListAsync<TResult>(Expression<Func<TModel, TResult>> selector, CancellationToken cancellationToken = default) where TResult : class;
    Task<Dictionary<TKey, List<TModel>>> GroupByAsync<TKey>(Expression<Func<TModel, TKey>> keySelector, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Bulk-updates every row matching the current WHERE clause. The setter expression must be a
    /// member-init or anonymous-new shape like <c>candidate =&gt; new { Property = newValue }</c>;
    /// each assigned property maps to a SET column-value pair, with registered value converters
    /// applied automatically. Returns the number of affected rows.
    /// </summary>
    Task<int> UpdateAsync(Expression<Func<TModel, TModel>> setterExpression, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-deletes every row matching the current WHERE clause. Honours soft-delete metadata —
    /// if the model has <c>[SoftDelete]</c> applied, this still emits a hard DELETE; pair with
    /// <c>OnlyTrashed()</c> to scope to already-soft-deleted rows when you want to purge them.
    /// Returns the number of affected rows.
    /// </summary>
    Task<int> DeleteAsync(CancellationToken cancellationToken = default);
}
