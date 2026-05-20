using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Query;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart.Loading;

internal sealed class IncludableSmartQuery<TRoot, TCurrent> : IIncludableSmartQuery<TRoot, TCurrent>
    where TRoot : class, new()
{
    private readonly ISmartQuery<TRoot> innerQuery;
    private readonly string parentNavigationPath;

    public IncludableSmartQuery(ISmartQuery<TRoot> innerQuery, string parentNavigationPath)
    {
        this.innerQuery = innerQuery;
        this.parentNavigationPath = parentNavigationPath;
    }

    public IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, TNext?>> navigation)
        where TNext : class
    {
        string memberName = ExtractMemberName(navigation);
        string combinedPath = parentNavigationPath + "." + memberName;
        innerQuery.Include(combinedPath);
        return new IncludableSmartQuery<TRoot, TNext>(innerQuery, combinedPath);
    }

    public IIncludableSmartQuery<TRoot, TNext> ThenInclude<TNext>(Expression<Func<TCurrent, IEnumerable<TNext>>> navigation)
        where TNext : class
    {
        string memberName = ExtractMemberName(navigation);
        string combinedPath = parentNavigationPath + "." + memberName;
        innerQuery.Include(combinedPath);
        return new IncludableSmartQuery<TRoot, TNext>(innerQuery, combinedPath);
    }

    private static string ExtractMemberName(LambdaExpression lambda)
    {
        Expression body = lambda.Body;
        while (body is UnaryExpression unaryExpression
               && (unaryExpression.NodeType == ExpressionType.Convert
                   || unaryExpression.NodeType == ExpressionType.ConvertChecked))
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        throw new ArgumentException(
            "ThenInclude requires a direct member access (for example: x => x.Property).", nameof(lambda));
    }

    public ISmartQuery<TRoot> Schema(string schemaName) => innerQuery.Schema(schemaName);
    public ISmartQuery<TRoot> Where(Expression<Func<TRoot, bool>> predicate) => innerQuery.Where(predicate);
    public ISmartQuery<TRoot> Where(string columnName, string operatorSymbol, object value) => innerQuery.Where(columnName, operatorSymbol, value);
    public ISmartQuery<TRoot> WhereRaw(string rawSql, params object[] parameters) => innerQuery.WhereRaw(rawSql, parameters);
    public ISmartQuery<TRoot> WhereExists(IQueryBuilder subquery) => innerQuery.WhereExists(subquery);
    public ISmartQuery<TRoot> WhereNotExists(IQueryBuilder subquery) => innerQuery.WhereNotExists(subquery);
    public ISmartQuery<TRoot> WhereIn(Expression<Func<TRoot, object>> property, IQueryBuilder subquery) => innerQuery.WhereIn(property, subquery);
    public ISmartQuery<TRoot> WhereNotIn(Expression<Func<TRoot, object>> property, IQueryBuilder subquery) => innerQuery.WhereNotIn(property, subquery);
    public ISmartQuery<TRoot> WhereIn<TKey>(Expression<Func<TRoot, TKey>> property, IEnumerable<TKey> values) => innerQuery.WhereIn(property, values);
    public ISmartQuery<TRoot> WhereNotIn<TKey>(Expression<Func<TRoot, TKey>> property, IEnumerable<TKey> values) => innerQuery.WhereNotIn(property, values);
    public ISmartQuery<TRoot> OrWhere(Expression<Func<TRoot, bool>> predicate) => innerQuery.OrWhere(predicate);
    public ISmartQuery<TRoot> OrWhere(string columnName, string operatorSymbol, object value) => innerQuery.OrWhere(columnName, operatorSymbol, value);
    public ISmartQuery<TRoot> OrderBy(Expression<Func<TRoot, object>> keySelector) => innerQuery.OrderBy(keySelector);
    public ISmartQuery<TRoot> OrderByDescending(Expression<Func<TRoot, object>> keySelector) => innerQuery.OrderByDescending(keySelector);
    public ISmartQuery<TRoot> OrderByRaw(string rawSql) => innerQuery.OrderByRaw(rawSql);
    public ISmartQuery<TRoot> Skip(int count) => innerQuery.Skip(count);
    public ISmartQuery<TRoot> Take(int count) => innerQuery.Take(count);
    public ISmartQuery<TRoot> Include(Expression<Func<TRoot, object>> navigationProperty) => innerQuery.Include(navigationProperty);
    public ISmartQuery<TRoot> Include(string navigationPath) => innerQuery.Include(navigationPath);
    public ISmartQuery<TRoot> WithTrashed() => innerQuery.WithTrashed();
    public ISmartQuery<TRoot> OnlyTrashed() => innerQuery.OnlyTrashed();
    public ISmartQuery<TRoot> Join<TJoin>(string tableName, Expression<Func<TRoot, TJoin, bool>> joinPredicate) where TJoin : class, new() => innerQuery.Join(tableName, joinPredicate);
    public ISmartQuery<TRoot> LeftJoin<TJoin>(string tableName, Expression<Func<TRoot, TJoin, bool>> joinPredicate) where TJoin : class, new() => innerQuery.LeftJoin(tableName, joinPredicate);
    public ISmartQuery<TRoot> RightJoin<TJoin>(string tableName, Expression<Func<TRoot, TJoin, bool>> joinPredicate) where TJoin : class, new() => innerQuery.RightJoin(tableName, joinPredicate);
    public ISmartQuery<TRoot> GroupBy(params Expression<Func<TRoot, object>>[] keySelectors) => innerQuery.GroupBy(keySelectors);
    public ISmartQuery<TRoot> Having(string aggregateExpression, string operatorSymbol, object value) => innerQuery.Having(aggregateExpression, operatorSymbol, value);
    public ISmartQuery<TRoot> SelectDistinct() => innerQuery.SelectDistinct();
    public ISmartQuery<TRoot> WhereJsonContains(Expression<Func<TRoot, object>> property, string jsonPath, object value) => innerQuery.WhereJsonContains(property, jsonPath, value);
    public ISmartQuery<TRoot> WhereJsonExists(Expression<Func<TRoot, object>> property, string jsonPath) => innerQuery.WhereJsonExists(property, jsonPath);
    public ISmartQuery<TRoot> WhereJsonbContains(Expression<Func<TRoot, object>> property, object jsonObject) => innerQuery.WhereJsonbContains(property, jsonObject);
    public ISmartQuery<TRoot> WhereJsonbPathExists(Expression<Func<TRoot, object>> property, string jsonPath) => innerQuery.WhereJsonbPathExists(property, jsonPath);
    public ISmartQuery<TRoot> WhereJsonbHasKey(Expression<Func<TRoot, object>> property, string key) => innerQuery.WhereJsonbHasKey(property, key);
    public ISmartQuery<TRoot> WhereJsonbHasAnyKeys(Expression<Func<TRoot, object>> property, params string[] keys) => innerQuery.WhereJsonbHasAnyKeys(property, keys);
    public ISmartQuery<TRoot> WhereJsonbHasAllKeys(Expression<Func<TRoot, object>> property, params string[] keys) => innerQuery.WhereJsonbHasAllKeys(property, keys);
    public ISmartQuery<TRoot> WhereArrayContains<TElement>(Expression<Func<TRoot, object>> property, params TElement[] elements) => innerQuery.WhereArrayContains(property, elements);
    public ISmartQuery<TRoot> WhereArrayContainedBy<TElement>(Expression<Func<TRoot, object>> property, params TElement[] elements) => innerQuery.WhereArrayContainedBy(property, elements);
    public ISmartQuery<TRoot> WhereArrayOverlaps<TElement>(Expression<Func<TRoot, object>> property, params TElement[] elements) => innerQuery.WhereArrayOverlaps(property, elements);
    public ISmartQuery<TRoot> WhereFullTextSearch(Expression<Func<TRoot, object>> property, string searchQuery, string language = "english") => innerQuery.WhereFullTextSearch(property, searchQuery, language);
    public ISmartQuery<TRoot> OrderByFullTextRank(Expression<Func<TRoot, object>> property, string searchQuery, string language = "english") => innerQuery.OrderByFullTextRank(property, searchQuery, language);
    public ISmartQuery<TRoot> WhereVectorSimilarity(Expression<Func<TRoot, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance, float threshold = 0.5f) => innerQuery.WhereVectorSimilarity(property, vector, vectorOperator, threshold);
    public ISmartQuery<TRoot> OrderByVectorDistance(Expression<Func<TRoot, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance) => innerQuery.OrderByVectorDistance(property, vector, vectorOperator);
    public ISmartQuery<TRoot> With(string cteName, IQueryBuilder cteQuery) => innerQuery.With(cteName, cteQuery);
    public ISmartQuery<TRoot> WithRecursive(string cteName, IQueryBuilder cteQuery) => innerQuery.WithRecursive(cteName, cteQuery);

    public Task<TRoot?> FirstOrDefaultAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.FirstOrDefaultAsync(cancellationToken);
    public Task<TRoot> FirstAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.FirstAsync(cancellationToken);
    public Task<List<TRoot>> ToListAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.ToListAsync(cancellationToken);
    public Task<int> CountAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.CountAsync(cancellationToken);
    public Task<bool> AnyAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.AnyAsync(cancellationToken);
    public Task<TResult?> MaxAsync<TResult>(Expression<Func<TRoot, TResult>> selector, System.Threading.CancellationToken cancellationToken = default) => innerQuery.MaxAsync(selector, cancellationToken);
    public Task<TResult?> MinAsync<TResult>(Expression<Func<TRoot, TResult>> selector, System.Threading.CancellationToken cancellationToken = default) => innerQuery.MinAsync(selector, cancellationToken);
    public Task<decimal> SumAsync(Expression<Func<TRoot, decimal>> selector, System.Threading.CancellationToken cancellationToken = default) => innerQuery.SumAsync(selector, cancellationToken);
    public Task<double> AverageAsync(Expression<Func<TRoot, double>> selector, System.Threading.CancellationToken cancellationToken = default) => innerQuery.AverageAsync(selector, cancellationToken);
    public Task<List<TResult>> ToListAsync<TResult>(Expression<Func<TRoot, TResult>> selector, System.Threading.CancellationToken cancellationToken = default) where TResult : class => innerQuery.ToListAsync(selector, cancellationToken);
    public Task<Dictionary<TKey, List<TRoot>>> GroupByAsync<TKey>(Expression<Func<TRoot, TKey>> keySelector, System.Threading.CancellationToken cancellationToken = default) where TKey : notnull => innerQuery.GroupByAsync(keySelector, cancellationToken);
    public Task<int> UpdateAsync(Expression<Func<TRoot, TRoot>> setterExpression, System.Threading.CancellationToken cancellationToken = default) => innerQuery.UpdateAsync(setterExpression, cancellationToken);
    public Task<int> DeleteAsync(System.Threading.CancellationToken cancellationToken = default) => innerQuery.DeleteAsync(cancellationToken);
}
