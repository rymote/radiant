using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Smart.Expressions;
using Rymote.Radiant.Smart.Loading;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Smart.Query;

public sealed class SmartQuery<TModel> : ISmartQuery<TModel> where TModel : class, new()
{
    private readonly IDbConnection databaseConnection;
    private readonly IModelMetadata modelMetadata;
    private readonly SelectBuilder selectBuilder;
    private readonly List<Expression<Func<TModel, object>>> includeExpressions;
    private bool includeSoftDeleted;
    private bool onlySoftDeleted;
    private int? skipCount;
    private int? takeCount;
    private readonly IRelationshipLoader relationshipLoader;

    public SmartQuery(IDbConnection databaseConnection, IModelMetadata modelMetadata)
    {
        this.databaseConnection = databaseConnection;
        this.modelMetadata = modelMetadata;
        this.selectBuilder = new SelectBuilder();
        this.includeExpressions = new List<Expression<Func<TModel, object>>>();
        this.includeSoftDeleted = false;
        this.onlySoftDeleted = false;
        this.skipCount = null;
        this.takeCount = null;
        IModelMetadataCache metadataCache = SmartModel.GetMetadataCache();
        this.relationshipLoader = new RelationshipLoader<TModel>(modelMetadata, databaseConnection, metadataCache);

        InitializeSelectBuilder();
    }

    private void InitializeSelectBuilder()
    {
        List<ISqlExpression> columnExpressions = modelMetadata.Properties.Values
            .Select(property => new RawSqlExpression($"\"{property.ColumnName}\" AS \"{property.PropertyName}\"") as ISqlExpression)
            .ToList();

        selectBuilder.Select(columnExpressions.ToArray())
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        if (modelMetadata.HasSoftDelete && !includeSoftDeleted && modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(modelMetadata.DeletedAtPropertyName);
            selectBuilder.WhereNull(deletedAtColumnName);
        }
    }

    public ISmartQuery<TModel> Where(Expression<Func<TModel, bool>> predicate)
    {
        WhereExpressionVisitor visitor = new WhereExpressionVisitor(modelMetadata);
        visitor.Visit(predicate);

        foreach ((string columnName, string operatorSymbol, object value) in visitor.Conditions)
        {
            selectBuilder.Where(columnName, operatorSymbol, value);
        }

        return this;
    }

    public ISmartQuery<TModel> Where(string columnName, string operatorSymbol, object value)
    {
        selectBuilder.Where(columnName, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> WhereRaw(string rawSql, params object[] parameters)
    {
        RawSqlExpression rawExpression = new RawSqlExpression(rawSql);
        string expressionString = SmartQueryExtensions.BuildExpressionString(rawExpression);
        selectBuilder.Where(expressionString, SqlKeywords.EQUALS.Trim(), parameters.Length > 0 ? parameters[0] : true);
        return this;
    }

    public ISmartQuery<TModel> WhereExists(IQueryBuilder subquery)
    {
        selectBuilder.WhereExists(subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereNotExists(IQueryBuilder subquery)
    {
        selectBuilder.WhereNotExists(subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery)
    {
        string columnName = GetColumnNameFromExpression(property);
        selectBuilder.Where(columnName, "IN", subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereNotIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery)
    {
        string columnName = GetColumnNameFromExpression(property);
        selectBuilder.Where(columnName, "NOT IN", subquery);
        return this;
    }

    public ISmartQuery<TModel> OrWhere(Expression<Func<TModel, bool>> predicate)
    {
        WhereExpressionVisitor visitor = new WhereExpressionVisitor(modelMetadata);
        visitor.Visit(predicate);

        foreach ((string columnName, string operatorSymbol, object value) in visitor.Conditions)
        {
            selectBuilder.Or(columnName, operatorSymbol, value);
        }

        return this;
    }

    public ISmartQuery<TModel> OrWhere(string columnName, string operatorSymbol, object value)
    {
        selectBuilder.Or(columnName, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> OrderBy(Expression<Func<TModel, object>> keySelector)
    {
        string columnName = GetColumnNameFromExpression(keySelector);
        selectBuilder.OrderBy(columnName, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> OrderByDescending(Expression<Func<TModel, object>> keySelector)
    {
        string columnName = GetColumnNameFromExpression(keySelector);
        selectBuilder.OrderBy(columnName, SortDirection.Descending);
        return this;
    }

    public ISmartQuery<TModel> OrderByRaw(string rawSql)
    {
        selectBuilder.OrderBy(rawSql, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> Skip(int count)
    {
        skipCount = count;
        return this;
    }

    public ISmartQuery<TModel> Take(int count)
    {
        takeCount = count;
        return this;
    }

    public ISmartQuery<TModel> Include(Expression<Func<TModel, object>> navigationProperty)
    {
        includeExpressions.Add(navigationProperty);
        return this;
    }

    public ISmartQuery<TModel> WithTrashed()
    {
        includeSoftDeleted = true;
        return this;
    }

    public ISmartQuery<TModel> OnlyTrashed()
    {
        if (!modelMetadata.HasSoftDelete)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");
        }

        onlySoftDeleted = true;
        if (modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(modelMetadata.DeletedAtPropertyName);
            selectBuilder.Where(deletedAtColumnName, "IS NOT", null);
        }
        return this;
    }

    public async Task<TModel?> FirstOrDefaultAsync()
    {
        ApplyLimitOffset();
        if (takeCount == null)
        {
            selectBuilder.Limit(1);
        }
        
        QueryExecutor executor = new QueryExecutor(databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(selectBuilder.Build());
        
        if (includeExpressions.Count > 0)
        {
            await LoadRelationshipsAsync(results.ToList());
        }
        
        return results.FirstOrDefault();
    }

    public async Task<TModel> FirstAsync()
    {
        TModel? result = await FirstOrDefaultAsync();
        if (result == null)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }
        return result;
    }

    public async Task<List<TModel>> ToListAsync()
    {
        ApplyLimitOffset();
        
        QueryExecutor executor = new QueryExecutor(databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(selectBuilder.Build());
        List<TModel> resultList = results.ToList();
        
        if (includeExpressions.Count > 0)
        {
            await LoadRelationshipsAsync(resultList);
        }
        
        return resultList;
    }

    public async Task<int> CountAsync()
    {
        SelectBuilder countBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("COUNT(*)"))
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        if (modelMetadata.HasSoftDelete && !includeSoftDeleted && !onlySoftDeleted && modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(modelMetadata.DeletedAtPropertyName);
            countBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        return await executor.QuerySingleAsync<int>(countBuilder.Build());
    }

    public async Task<bool> AnyAsync()
    {
        selectBuilder.Limit(1);
        QueryExecutor executor = new QueryExecutor(databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(selectBuilder.Build());
        return results.Any();
    }

    public ISmartQuery<TModel> Join<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
        {
            selectBuilder.InnerJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        }
        
        return this;
    }

    public ISmartQuery<TModel> LeftJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
        {
            selectBuilder.LeftJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        }
        
        return this;
    }

    public ISmartQuery<TModel> RightJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
        {
            selectBuilder.RightJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        }
        
        return this;
    }

    public ISmartQuery<TModel> GroupBy(params Expression<Func<TModel, object>>[] keySelectors)
    {
        string[] columnNames = keySelectors
            .Select(selector => GetColumnNameFromExpression(selector))
            .ToArray();
            
        selectBuilder.GroupBy(columnNames);
        return this;
    }

    public ISmartQuery<TModel> Having(string aggregateExpression, string operatorSymbol, object value)
    {
        selectBuilder.Having(aggregateExpression, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> SelectDistinct()
    {
        List<ISqlExpression> columnExpressions = modelMetadata.Properties.Values
            .Select(property => new ColumnExpression(property.ColumnName) as ISqlExpression)
            .ToList();

        selectBuilder.SelectDistinct(columnExpressions.ToArray());
        return this;
    }

    public ISmartQuery<TModel> WhereJsonContains(Expression<Func<TModel, object>> property, string jsonPath, object value)
    {
        string columnName = GetColumnNameFromExpression(property);
        JsonExpression jsonExpression = new JsonExpression(
            new ColumnExpression(columnName),
            JsonOperator.Contains,
            $"{{\"{jsonPath}\": \"{value}\"}}"
        );
    
        selectBuilder.WhereBooleanExpression(jsonExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereJsonExists(Expression<Func<TModel, object>> property, string jsonPath)
    {
        string columnName = GetColumnNameFromExpression(property);
        JsonExpression jsonExpression = new JsonExpression(
            new ColumnExpression(columnName),
            JsonOperator.PathExists,
            jsonPath
        );
    
        selectBuilder.WhereBooleanExpression(jsonExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereFullTextSearch(Expression<Func<TModel, object>> property, string searchQuery, string language = "english")
    {
        string columnName = GetColumnNameFromExpression(property);
        FullTextExpression tsVectorExpression = FullTextExpression.ToTsVector(language, new ColumnExpression(columnName));
        FullTextExpression tsQueryExpression = FullTextExpression.PlainToTsQuery(language, searchQuery);
        FullTextMatchExpression matchExpression = new FullTextMatchExpression(tsVectorExpression, tsQueryExpression);
    
        selectBuilder.WhereBooleanExpression(matchExpression);
        return this;
    }

    public ISmartQuery<TModel> OrderByFullTextRank(Expression<Func<TModel, object>> property, string searchQuery, string language = "english")
    {
        string columnName = GetColumnNameFromExpression(property);
        FullTextExpression tsVectorExpression = FullTextExpression.ToTsVector(language, new ColumnExpression(columnName));
        FullTextExpression tsQueryExpression = FullTextExpression.PlainToTsQuery(language, searchQuery);
        FullTextExpression rankExpression = FullTextExpression.TsRank(tsVectorExpression, tsQueryExpression);
        
        selectBuilder.OrderByExpression(rankExpression, SortDirection.Descending);
        return this;
    }

    public ISmartQuery<TModel> WhereVectorSimilarity(Expression<Func<TModel, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance, float threshold = 0.5f)
    {
        string columnName = GetColumnNameFromExpression(property);
        VectorExpression vectorExpression = new VectorExpression(
            new ColumnExpression(columnName),
            vectorOperator,
            new VectorLiteralExpression(vector)
        );
        
        selectBuilder.WhereExpression(vectorExpression, "<", threshold);
        return this;
    }

    public ISmartQuery<TModel> OrderByVectorDistance(Expression<Func<TModel, object>> property, float[] vector, VectorOperator vectorOperator = VectorOperator.CosineDistance)
    {
        string columnName = GetColumnNameFromExpression(property);
        VectorExpression vectorExpression = new VectorExpression(
            new ColumnExpression(columnName),
            vectorOperator,
            new VectorLiteralExpression(vector)
        );
        
        selectBuilder.OrderByExpression(vectorExpression, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> With(string cteName, IQueryBuilder cteQuery)
    {
        selectBuilder.With(cteName, cteQuery);
        return this;
    }

    public ISmartQuery<TModel> WithRecursive(string cteName, IQueryBuilder cteQuery)
    {
        selectBuilder.WithRecursive(cteName, cteQuery);
        return this;
    }

    public async Task<TResult?> MaxAsync<TResult>(Expression<Func<TModel, TResult>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("MAX", new ColumnExpression(columnName)))
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        return await executor.QuerySingleAsync<TResult>(aggregateBuilder.Build());
    }

    public async Task<TResult?> MinAsync<TResult>(Expression<Func<TModel, TResult>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("MIN", new ColumnExpression(columnName)))
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        return await executor.QuerySingleAsync<TResult>(aggregateBuilder.Build());
    }

    public async Task<decimal> SumAsync(Expression<Func<TModel, decimal>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("COALESCE", 
                new FunctionExpression("SUM", new ColumnExpression(columnName)), 
                new LiteralExpression(0)))
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        return await executor.QuerySingleAsync<decimal>(aggregateBuilder.Build());
    }

    public async Task<double> AverageAsync(Expression<Func<TModel, double>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("AVG", new ColumnExpression(columnName)))
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        return await executor.QuerySingleAsync<double>(aggregateBuilder.Build());
    }

    public async Task<List<TResult>> ToListAsync<TResult>(Expression<Func<TModel, TResult>> selector) where TResult : class
    {
        ApplyLimitOffset();
        
        QueryExecutor executor = new QueryExecutor(databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(selectBuilder.Build());
        
        Func<TModel, TResult> compiledSelector = selector.Compile();
        return results.Select(compiledSelector).ToList();
    }

    public async Task<Dictionary<TKey, List<TModel>>> GroupByAsync<TKey>(Expression<Func<TModel, TKey>> keySelector) where TKey : notnull
    {
        List<TModel> results = await ToListAsync();
        Func<TModel, TKey> compiledKeySelector = keySelector.Compile();
        
        return results
            .GroupBy(compiledKeySelector)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    private string GetColumnNameFromExpression(Expression<Func<TModel, object>> expression)
    {
        MemberExpression? memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression");
        }

        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata == null)
        {
            throw new ArgumentException($"Property {propertyName} not found in model metadata");
        }

        return propertyMetadata.ColumnName;
    }

    private string GetColumnNameFromExpression<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        MemberExpression? memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression");
        }

        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata == null)
        {
            throw new ArgumentException($"Property {propertyName} not found in model metadata");
        }

        return propertyMetadata.ColumnName;
    }

    private string GetColumnNameFromPropertyName(string propertyName)
    {
        IPropertyMetadata? propertyMetadata = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata != null)
        {
            return propertyMetadata.ColumnName;
        }

        return ConvertPropertyNameToColumnName(propertyName);
    }

    private string ConvertPropertyNameToColumnName(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) => 
            index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }

    private void ApplyLimitOffset()
    {
        if (takeCount.HasValue)
        {
            selectBuilder.Limit(takeCount.Value, skipCount);
        }
    }

    private async Task LoadRelationshipsAsync(List<TModel> models)
    {
        if (models.Count == 0) return;

        foreach (Expression<Func<TModel, object>> includeExpression in includeExpressions)
        {
            await relationshipLoader.LoadRelationshipAsync(models, includeExpression);
        }
    }

    private void ApplySoftDeleteFilter(SelectBuilder builder)
    {
        if (modelMetadata.HasSoftDelete && !includeSoftDeleted && !onlySoftDeleted && modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(modelMetadata.DeletedAtPropertyName);
            builder.WhereNull(deletedAtColumnName);
        }
    }

    private string GetVectorOperatorSymbol(VectorOperator vectorOperator)
    {
        return vectorOperator switch
        {
            VectorOperator.L2Distance => SqlKeywords.VECTOR_L2_DISTANCE,
            VectorOperator.InnerProduct => SqlKeywords.VECTOR_INNER_PRODUCT,
            VectorOperator.CosineDistance => SqlKeywords.VECTOR_COSINE_DISTANCE,
            VectorOperator.CosineSimilarity => SqlKeywords.VECTOR_COSINE_DISTANCE,
            _ => SqlKeywords.VECTOR_COSINE_DISTANCE
        };
    }

    public ISmartQuery<TModel> WhereJsonbContains(Expression<Func<TModel, object>> property, object jsonObject)
    {
        string columnName = GetColumnNameFromExpression(property);
        string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonObject);
        JsonbExpression jsonbExpression = new JsonbExpression(
            new ColumnExpression(columnName),
            JsonbOperator.StrictContains,
            new LiteralExpression(jsonString)
        );
    
        selectBuilder.WhereBooleanExpression(jsonbExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereJsonbPathExists(Expression<Func<TModel, object>> property, string jsonPath)
    {
        string columnName = GetColumnNameFromExpression(property);
        JsonbExpression jsonbExpression = new JsonbExpression(
            new ColumnExpression(columnName),
            JsonbOperator.PathExists,
            new LiteralExpression($"$.{jsonPath}")
        );
    
        selectBuilder.WhereBooleanExpression(jsonbExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereJsonbHasKey(Expression<Func<TModel, object>> property, string key)
    {
        string columnName = GetColumnNameFromExpression(property);
        JsonbExpression jsonbExpression = new JsonbExpression(
            new ColumnExpression(columnName),
            JsonbOperator.HasKey,
            new LiteralExpression(key)
        );
    
        selectBuilder.WhereBooleanExpression(jsonbExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereJsonbHasAnyKeys(Expression<Func<TModel, object>> property, params string[] keys)
    {
        string columnName = GetColumnNameFromExpression(property);
        ArrayLiteralExpression arrayLiteral = new ArrayLiteralExpression(keys.Cast<object>().ToArray());
        JsonbExpression jsonbExpression = new JsonbExpression(
            new ColumnExpression(columnName),
            JsonbOperator.HasAnyKey,
            arrayLiteral
        );
    
        selectBuilder.WhereBooleanExpression(jsonbExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereJsonbHasAllKeys(Expression<Func<TModel, object>> property, params string[] keys)
    {
        string columnName = GetColumnNameFromExpression(property);
        ArrayLiteralExpression arrayLiteral = new ArrayLiteralExpression(keys.Cast<object>().ToArray());
        JsonbExpression jsonbExpression = new JsonbExpression(
            new ColumnExpression(columnName),
            JsonbOperator.HasAllKeys,
            arrayLiteral
        );
    
        selectBuilder.WhereBooleanExpression(jsonbExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereArrayContains<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements)
    {
        string columnName = GetColumnNameFromExpression(property);
        ArrayLiteralExpression arrayLiteral = new ArrayLiteralExpression(elements.Cast<object>().ToArray());
        ArrayExpression arrayExpression = new ArrayExpression(
            new ColumnExpression(columnName),
            ArrayOperator.Contains,
            arrayLiteral
        );
    
        selectBuilder.WhereBooleanExpression(arrayExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereArrayContainedBy<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements)
    {
        string columnName = GetColumnNameFromExpression(property);
        ArrayLiteralExpression arrayLiteral = new ArrayLiteralExpression(elements.Cast<object>().ToArray());
        ArrayExpression arrayExpression = new ArrayExpression(
            new ColumnExpression(columnName),
            ArrayOperator.ContainedBy,
            arrayLiteral
        );
    
        selectBuilder.WhereBooleanExpression(arrayExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereArrayOverlaps<TElement>(Expression<Func<TModel, object>> property, params TElement[] elements)
    {
        string columnName = GetColumnNameFromExpression(property);
        ArrayLiteralExpression arrayLiteral = new ArrayLiteralExpression(elements.Cast<object>().ToArray());
        ArrayExpression arrayExpression = new ArrayExpression(
            new ColumnExpression(columnName),
            ArrayOperator.Overlap,
            arrayLiteral
        );
    
        selectBuilder.WhereBooleanExpression(arrayExpression);
        return this;
    }
}