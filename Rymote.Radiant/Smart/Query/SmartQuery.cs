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
    private readonly IDbConnection _databaseConnection;
    private readonly IModelMetadata _modelMetadata;
    private readonly SelectBuilder _selectBuilder;
    private readonly List<Expression<Func<TModel, object>>> _includeExpressions;
    private readonly List<string> _includePaths = new List<string>();
    private bool _includeSoftDeleted;
    private bool _onlySoftDeleted;
    private int? _skipCount;
    private int? _takeCount;
    private readonly RelationshipLoader<TModel> _relationshipLoader;
    private string? _schemaOverride;

    public SmartQuery(IDbConnection databaseConnection, IModelMetadata modelMetadata)
    {
        _databaseConnection = databaseConnection;
        _modelMetadata = modelMetadata;
        _selectBuilder = new SelectBuilder();
        _includeExpressions = new List<Expression<Func<TModel, object>>>();
        _includeSoftDeleted = false;
        _onlySoftDeleted = false;
        _skipCount = null;
        _takeCount = null;
        IModelMetadataCache metadataCache = SmartModel.GetMetadataCache();
        _relationshipLoader = new RelationshipLoader<TModel>(modelMetadata, databaseConnection, metadataCache);
        _schemaOverride = null;

        InitializeSelectBuilder();
    }

    private void InitializeSelectBuilder()
    {
        List<ISqlExpression> columnExpressions = _modelMetadata.Properties.Values
            .Select(property => new RawSqlExpression($"\"{property.ColumnName}\" AS \"{property.PropertyName}\"") as ISqlExpression)
            .ToList();

        _selectBuilder.Select(columnExpressions.ToArray())
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        if (_modelMetadata.HasSoftDelete && !_includeSoftDeleted && _modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(_modelMetadata.DeletedAtPropertyName);
            _selectBuilder.WhereNull(deletedAtColumnName);
        }
    }

    public ISmartQuery<TModel> Schema(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
            throw new ArgumentException("schemaName cannot be null or empty", nameof(schemaName));

        _schemaOverride = schemaName;
        _relationshipLoader.SchemaOverride = schemaName;

        // Update the FROM clause to use the new schema. Safe to call multiple times.
        _selectBuilder.From(_modelMetadata.TableName, _schemaOverride);
        return this;
    }

    public ISmartQuery<TModel> Where(Expression<Func<TModel, bool>> predicate)
    {
        WhereExpressionVisitor visitor = new WhereExpressionVisitor(_modelMetadata);
        visitor.Visit(predicate);

        foreach ((string columnName, string operatorSymbol, object value) in visitor.Conditions)
            _selectBuilder.Where(columnName, operatorSymbol, value);

        return this;
    }

    public ISmartQuery<TModel> Where(string columnName, string operatorSymbol, object value)
    {
        _selectBuilder.Where(columnName, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> WhereRaw(string rawSql, params object[] parameters)
    {
        RawSqlExpression rawExpression = new RawSqlExpression(rawSql);
        string expressionString = SmartQueryExtensions.BuildExpressionString(rawExpression);
        _selectBuilder.Where(expressionString, SqlKeywords.EQUALS.Trim(), parameters.Length > 0 ? parameters[0] : true);
        return this;
    }

    public ISmartQuery<TModel> WhereExists(IQueryBuilder subquery)
    {
        _selectBuilder.WhereExists(subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereNotExists(IQueryBuilder subquery)
    {
        _selectBuilder.WhereNotExists(subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery)
    {
        string columnName = GetColumnNameFromExpression(property);
        _selectBuilder.Where(columnName, "IN", subquery);
        return this;
    }

    public ISmartQuery<TModel> WhereNotIn(Expression<Func<TModel, object>> property, IQueryBuilder subquery)
    {
        string columnName = GetColumnNameFromExpression(property);
        _selectBuilder.Where(columnName, "NOT IN", subquery);
        return this;
    }

    public ISmartQuery<TModel> OrWhere(Expression<Func<TModel, bool>> predicate)
    {
        WhereExpressionVisitor visitor = new WhereExpressionVisitor(_modelMetadata);
        visitor.Visit(predicate);

        foreach ((string columnName, string operatorSymbol, object value) in visitor.Conditions)
            _selectBuilder.Or(columnName, operatorSymbol, value);

        return this;
    }

    public ISmartQuery<TModel> OrWhere(string columnName, string operatorSymbol, object value)
    {
        _selectBuilder.Or(columnName, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> OrderBy(Expression<Func<TModel, object>> keySelector)
    {
        string columnName = GetColumnNameFromExpression(keySelector);
        _selectBuilder.OrderBy(columnName, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> OrderByDescending(Expression<Func<TModel, object>> keySelector)
    {
        string columnName = GetColumnNameFromExpression(keySelector);
        _selectBuilder.OrderBy(columnName, SortDirection.Descending);
        return this;
    }

    public ISmartQuery<TModel> OrderByRaw(string rawSql)
    {
        _selectBuilder.OrderBy(rawSql, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> Skip(int count)
    {
        _skipCount = count;
        return this;
    }

    public ISmartQuery<TModel> Take(int count)
    {
        _takeCount = count;
        return this;
    }

    public ISmartQuery<TModel> Include(Expression<Func<TModel, object>> navigationProperty)
    {
        _includeExpressions.Add(navigationProperty);
        return this;
    }

    public ISmartQuery<TModel> Include(string navigationPath)
    {
        if (string.IsNullOrWhiteSpace(navigationPath))
            throw new ArgumentException("navigationPath cannot be null or empty", nameof(navigationPath));
        
        _includePaths.Add(navigationPath);
        return this;
    }

    public ISmartQuery<TModel> WithTrashed()
    {
        _includeSoftDeleted = true;
        return this;
    }

    public ISmartQuery<TModel> OnlyTrashed()
    {
        if (!_modelMetadata.HasSoftDelete)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");

        _onlySoftDeleted = true;
        if (_modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(_modelMetadata.DeletedAtPropertyName);
            _selectBuilder.Where(deletedAtColumnName, "IS NOT", null);
        }
        
        return this;
    }

    public async Task<TModel?> FirstOrDefaultAsync()
    {
        ApplyLimitOffset();
        
        if (_takeCount == null)
            _selectBuilder.Limit(1);
        
        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(_selectBuilder.Build());
        
        if (_includeExpressions.Count > 0)
            await LoadRelationshipsAsync(results.ToList());
        
        if (_includePaths.Count > 0)
            await LoadNestedRelationshipsAsync(results.ToList());
        
        return results.FirstOrDefault();
    }

    public async Task<TModel> FirstAsync()
    {
        TModel? result = await FirstOrDefaultAsync();
        if (result == null)
            throw new InvalidOperationException("Sequence contains no elements");
        
        return result;
    }

    public async Task<List<TModel>> ToListAsync()
    {
        ApplyLimitOffset();
        
        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(_selectBuilder.Build());
        List<TModel> resultList = results.ToList();
        
        if (_includeExpressions.Count > 0)
            await LoadRelationshipsAsync(resultList);
        
        if (_includePaths.Count > 0)
            await LoadNestedRelationshipsAsync(resultList);
        
        return resultList;
    }

    public async Task<int> CountAsync()
    {
        SelectBuilder countBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("COUNT(*)"))
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        if (_modelMetadata.HasSoftDelete && !_includeSoftDeleted && !_onlySoftDeleted && _modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(_modelMetadata.DeletedAtPropertyName);
            countBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        return await executor.QuerySingleAsync<int>(countBuilder.Build());
    }

    public async Task<bool> AnyAsync()
    {
        _selectBuilder.Limit(1);
        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(_selectBuilder.Build());
        return results.Any();
    }

    public ISmartQuery<TModel> Join<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(_modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
            _selectBuilder.InnerJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        
        return this;
    }

    public ISmartQuery<TModel> LeftJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(_modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
            _selectBuilder.LeftJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        
        return this;
    }

    public ISmartQuery<TModel> RightJoin<TJoin>(string tableName, Expression<Func<TModel, TJoin, bool>> joinPredicate) where TJoin : class, new()
    {
        JoinExpressionVisitor<TModel, TJoin> visitor = new JoinExpressionVisitor<TModel, TJoin>(_modelMetadata, SmartModel.GetMetadataCache().GetMetadata<TJoin>());
        visitor.Visit(joinPredicate);
        
        if (visitor.LeftColumn != null && visitor.RightColumn != null)
            _selectBuilder.RightJoin(tableName, visitor.LeftColumn, visitor.RightColumn);
        
        return this;
    }

    public ISmartQuery<TModel> GroupBy(params Expression<Func<TModel, object>>[] keySelectors)
    {
        string[] columnNames = keySelectors
            .Select(selector => GetColumnNameFromExpression(selector))
            .ToArray();
            
        _selectBuilder.GroupBy(columnNames);
        return this;
    }

    public ISmartQuery<TModel> Having(string aggregateExpression, string operatorSymbol, object value)
    {
        _selectBuilder.Having(aggregateExpression, operatorSymbol, value);
        return this;
    }

    public ISmartQuery<TModel> SelectDistinct()
    {
        List<ISqlExpression> columnExpressions = _modelMetadata.Properties.Values
            .Select(property => new ColumnExpression(property.ColumnName) as ISqlExpression)
            .ToList();

        _selectBuilder.SelectDistinct(columnExpressions.ToArray());
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
    
        _selectBuilder.WhereBooleanExpression(jsonExpression);
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
    
        _selectBuilder.WhereBooleanExpression(jsonExpression);
        return this;
    }

    public ISmartQuery<TModel> WhereFullTextSearch(Expression<Func<TModel, object>> property, string searchQuery, string language = "english")
    {
        string columnName = GetColumnNameFromExpression(property);
        FullTextExpression tsVectorExpression = FullTextExpression.ToTsVector(language, new ColumnExpression(columnName));
        FullTextExpression tsQueryExpression = FullTextExpression.PlainToTsQuery(language, searchQuery);
        FullTextMatchExpression matchExpression = new FullTextMatchExpression(tsVectorExpression, tsQueryExpression);
    
        _selectBuilder.WhereBooleanExpression(matchExpression);
        return this;
    }

    public ISmartQuery<TModel> OrderByFullTextRank(Expression<Func<TModel, object>> property, string searchQuery, string language = "english")
    {
        string columnName = GetColumnNameFromExpression(property);
        FullTextExpression tsVectorExpression = FullTextExpression.ToTsVector(language, new ColumnExpression(columnName));
        FullTextExpression tsQueryExpression = FullTextExpression.PlainToTsQuery(language, searchQuery);
        FullTextExpression rankExpression = FullTextExpression.TsRank(tsVectorExpression, tsQueryExpression);
        
        _selectBuilder.OrderByExpression(rankExpression, SortDirection.Descending);
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
        
        _selectBuilder.WhereExpression(vectorExpression, "<", threshold);
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
        
        _selectBuilder.OrderByExpression(vectorExpression, SortDirection.Ascending);
        return this;
    }

    public ISmartQuery<TModel> With(string cteName, IQueryBuilder cteQuery)
    {
        _selectBuilder.With(cteName, cteQuery);
        return this;
    }

    public ISmartQuery<TModel> WithRecursive(string cteName, IQueryBuilder cteQuery)
    {
        _selectBuilder.WithRecursive(cteName, cteQuery);
        return this;
    }

    public async Task<TResult?> MaxAsync<TResult>(Expression<Func<TModel, TResult>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("MAX", new ColumnExpression(columnName)))
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        return await executor.QuerySingleAsync<TResult>(aggregateBuilder.Build());
    }

    public async Task<TResult?> MinAsync<TResult>(Expression<Func<TModel, TResult>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("MIN", new ColumnExpression(columnName)))
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        return await executor.QuerySingleAsync<TResult>(aggregateBuilder.Build());
    }

    public async Task<decimal> SumAsync(Expression<Func<TModel, decimal>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("COALESCE", 
                new FunctionExpression("SUM", new ColumnExpression(columnName)), 
                new LiteralExpression(0)))
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        return await executor.QuerySingleAsync<decimal>(aggregateBuilder.Build());
    }

    public async Task<double> AverageAsync(Expression<Func<TModel, double>> selector)
    {
        string columnName = GetColumnNameFromExpression(selector);
        SelectBuilder aggregateBuilder = new SelectBuilder()
            .Select(new FunctionExpression("AVG", new ColumnExpression(columnName)))
            .From(_modelMetadata.TableName, _schemaOverride ?? _modelMetadata.SchemaName);

        ApplySoftDeleteFilter(aggregateBuilder);

        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        return await executor.QuerySingleAsync<double>(aggregateBuilder.Build());
    }

    public async Task<List<TResult>> ToListAsync<TResult>(Expression<Func<TModel, TResult>> selector) where TResult : class
    {
        ApplyLimitOffset();
        
        QueryExecutor executor = new QueryExecutor(_databaseConnection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(_selectBuilder.Build());
        
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
            memberExpression = unaryExpression.Operand as MemberExpression;

        if (memberExpression == null)
            throw new ArgumentException("Expression must be a member expression");

        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = _modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata == null)
            throw new ArgumentException($"Property {propertyName} not found in model metadata");

        return propertyMetadata.ColumnName;
    }

    private string GetColumnNameFromExpression<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        MemberExpression? memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            memberExpression = unaryExpression.Operand as MemberExpression;

        if (memberExpression == null)
            throw new ArgumentException("Expression must be a member expression");

        string propertyName = memberExpression.Member.Name;
        IPropertyMetadata? propertyMetadata = _modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata == null)
            throw new ArgumentException($"Property {propertyName} not found in model metadata");

        return propertyMetadata.ColumnName;
    }

    private string GetColumnNameFromPropertyName(string propertyName)
    {
        IPropertyMetadata? propertyMetadata = _modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata != null)
            return propertyMetadata.ColumnName;

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
        if (_takeCount.HasValue)
            _selectBuilder.Limit(_takeCount.Value, _skipCount);
    }

    private async Task LoadRelationshipsAsync(List<TModel> models)
    {
        if (models.Count == 0) return;

        foreach (Expression<Func<TModel, object>> includeExpression in _includeExpressions)
            await _relationshipLoader.LoadRelationshipAsync(models, includeExpression);
    }
    
    private async Task LoadNestedRelationshipsAsync(List<TModel> models)
    {
        if (models.Count == 0) return;

        foreach (string path in _includePaths)
            await _relationshipLoader.LoadRelationshipAsync(models, path);
    }

    private void ApplySoftDeleteFilter(SelectBuilder builder)
    {
        if (_modelMetadata.HasSoftDelete && !_includeSoftDeleted && !_onlySoftDeleted && _modelMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(_modelMetadata.DeletedAtPropertyName);
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
    
        _selectBuilder.WhereBooleanExpression(jsonbExpression);
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
    
        _selectBuilder.WhereBooleanExpression(jsonbExpression);
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
    
        _selectBuilder.WhereBooleanExpression(jsonbExpression);
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
    
        _selectBuilder.WhereBooleanExpression(jsonbExpression);
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
    
        _selectBuilder.WhereBooleanExpression(jsonbExpression);
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
    
        _selectBuilder.WhereBooleanExpression(arrayExpression);
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
    
        _selectBuilder.WhereBooleanExpression(arrayExpression);
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
    
        _selectBuilder.WhereBooleanExpression(arrayExpression);
        return this;
    }
}