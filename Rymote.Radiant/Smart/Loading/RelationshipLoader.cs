using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart.Loading;

public sealed class RelationshipLoader<TModel> : IRelationshipLoader where TModel : class, new()
{
    private readonly IModelMetadata modelMetadata;
    private readonly IDbConnection databaseConnection;
    private readonly IModelMetadataCache metadataCache;

    public RelationshipLoader(IModelMetadata modelMetadata, IDbConnection databaseConnection, IModelMetadataCache metadataCache)
    {
        this.modelMetadata = modelMetadata;
        this.databaseConnection = databaseConnection;
        this.metadataCache = metadataCache;
    }

    public async Task LoadRelationshipAsync<TEntity>(List<TEntity> models, Expression<Func<TEntity, object>> navigationProperty) 
        where TEntity : class, new()
    {
        if (typeof(TEntity) != typeof(TModel))
            throw new InvalidOperationException($"RelationshipLoader for {typeof(TModel).Name} cannot load relationships for {typeof(TEntity).Name}");

        List<TModel> typedModels = models.Cast<TModel>().ToList();
        await LoadRelationshipInternalAsync(typedModels, navigationProperty as Expression<Func<TModel, object>>);
    }

    private async Task LoadRelationshipInternalAsync(List<TModel> models, Expression<Func<TModel, object>> navigationProperty)
    {
        if (models.Count == 0) return;

        string propertyName = GetPropertyName(navigationProperty);
        IRelationshipMetadata? relationship = modelMetadata.Relationships
            .FirstOrDefault(relationshipMetadata => relationshipMetadata.PropertyInfo.Name == propertyName);

        if (relationship == null)
            throw new InvalidOperationException($"Property {propertyName} is not a defined relationship on {typeof(TModel).Name}");

        switch (relationship.RelationshipType)
        {
            case RelationshipType.HasOne:
                await LoadHasOneRelationshipAsync(models, relationship);
                break;
            case RelationshipType.HasMany:
                await LoadHasManyRelationshipAsync(models, relationship);
                break;
            case RelationshipType.BelongsTo:
                await LoadBelongsToRelationshipAsync(models, relationship);
                break;
        }
    }

    private ISqlExpression[] GetSelectExpressions(IModelMetadata metadata)
    {
        List<ISqlExpression> expressions = new List<ISqlExpression>();
        
        foreach (IPropertyMetadata property in metadata.Properties.Values)
        {
            RawSqlExpression aliasedColumn = new RawSqlExpression($"\"{property.ColumnName}\" AS \"{property.PropertyName}\"");
            expressions.Add(aliasedColumn);
        }
        
        return expressions.ToArray();
    }

    private async Task LoadHasOneRelationshipAsync(List<TModel> models, IRelationshipMetadata relationship)
    {
        IModelMetadata relatedMetadata = metadataCache.GetMetadata(relationship.RelatedModelType);
        IPropertyMetadata? foreignKeyProperty = relatedMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == relationship.ForeignKeyPropertyName);

        if (foreignKeyProperty == null)
            throw new InvalidOperationException($"Foreign key property {relationship.ForeignKeyPropertyName} not found on {relationship.RelatedModelType.Name}");

        if (modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} must have a primary key for HasOne relationships");

        List<object> primaryKeyValues = models
            .Select(model => modelMetadata.PrimaryKey.PropertyInfo.GetValue(model))
            .Where(value => value != null)
            .Distinct()
            .ToList();

        if (primaryKeyValues.Count == 0) return;

        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(GetSelectExpressions(relatedMetadata))
            .From(relatedMetadata.TableName, relatedMetadata.SchemaName)
            .Where(foreignKeyProperty.ColumnName, "IN", primaryKeyValues.ToArray());

        if (relatedMetadata.HasSoftDelete && relatedMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(relatedMetadata, relatedMetadata.DeletedAtPropertyName);
            selectBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        
        MethodInfo loadMethod = GetType()
            .GetMethod(nameof(ExecuteRelatedQuery), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(relationship.RelatedModelType);
        
        List<object> relatedModels = await (Task<List<object>>)loadMethod.Invoke(this, new object[] { executor, selectBuilder })!;

        Dictionary<object, object> relatedModelsByForeignKey = relatedModels
            .ToDictionary(relatedModel => foreignKeyProperty.PropertyInfo.GetValue(relatedModel)!);

        foreach (TModel model in models)
        {
            object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
            if (primaryKeyValue != null && relatedModelsByForeignKey.TryGetValue(primaryKeyValue, out object? relatedModel))
                relationship.PropertyInfo.SetValue(model, relatedModel);
        }
    }

    private async Task LoadHasManyRelationshipAsync(List<TModel> models, IRelationshipMetadata relationship)
    {
        IModelMetadata relatedMetadata = metadataCache.GetMetadata(relationship.RelatedModelType);
        IPropertyMetadata? foreignKeyProperty = relatedMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == relationship.ForeignKeyPropertyName);

        if (foreignKeyProperty == null)
            throw new InvalidOperationException($"Foreign key property {relationship.ForeignKeyPropertyName} not found on {relationship.RelatedModelType.Name}");

        if (modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} must have a primary key for HasMany relationships");

        List<object> primaryKeyValues = models
            .Select(model => modelMetadata.PrimaryKey.PropertyInfo.GetValue(model))
            .Where(value => value != null)
            .Distinct()
            .ToList();

        if (primaryKeyValues.Count == 0) return;

        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(GetSelectExpressions(relatedMetadata))
            .From(relatedMetadata.TableName, relatedMetadata.SchemaName)
            .Where(foreignKeyProperty.ColumnName, "IN", primaryKeyValues.ToArray());

        if (relatedMetadata.HasSoftDelete && relatedMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(relatedMetadata, relatedMetadata.DeletedAtPropertyName);
            selectBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(databaseConnection);

        MethodInfo loadMethod = GetType()
            .GetMethod(nameof(ExecuteRelatedQuery), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(relationship.RelatedModelType);

        List<object> relatedModels = await (Task<List<object>>)loadMethod.Invoke(this, new object[] { executor, selectBuilder })!;
        
        Dictionary<object, List<object>> relatedModelsByForeignKey = new Dictionary<object, List<object>>();

        foreach (object relatedModel in relatedModels)
        {
            object? foreignKeyValue = foreignKeyProperty.PropertyInfo.GetValue(relatedModel);
            
            if (foreignKeyValue != null)
            {
                object normalizedKey = Convert.ChangeType(foreignKeyValue, modelMetadata.PrimaryKey.PropertyType);
        
                if (!relatedModelsByForeignKey.ContainsKey(normalizedKey))
                    relatedModelsByForeignKey[normalizedKey] = new List<object>();
            
                relatedModelsByForeignKey[normalizedKey].Add(relatedModel);
            }
        }
        
        Type listType = typeof(List<>).MakeGenericType(relationship.RelatedModelType);

        foreach (TModel model in models)
        {
            object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
            
            if (primaryKeyValue != null && relatedModelsByForeignKey.TryGetValue(primaryKeyValue, out List<object>? relatedList))
            {
                IList typedList = (IList)Activator.CreateInstance(listType)!;
                
                foreach (object relatedModel in relatedList)
                    typedList.Add(relatedModel);
                
                relationship.PropertyInfo.SetValue(model, typedList);
            }
            else
            {
                relationship.PropertyInfo.SetValue(model, Activator.CreateInstance(listType));
            }
        }
    }

    private async Task<List<object>> ExecuteRelatedQuery<TRelated>(QueryExecutor executor, SelectBuilder selectBuilder)
        where TRelated : class
    {
        IEnumerable<TRelated> results = await executor.QueryAsync<TRelated>(selectBuilder.Build());
        return results.Cast<object>().ToList();
    }
    
    private async Task LoadBelongsToRelationshipAsync(List<TModel> models, IRelationshipMetadata relationship)
    {
        IPropertyMetadata? localForeignKeyProperty = modelMetadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == relationship.ForeignKeyPropertyName);

        if (localForeignKeyProperty == null)
            throw new InvalidOperationException($"Foreign key property {relationship.ForeignKeyPropertyName} not found on {typeof(TModel).Name}");

        IModelMetadata relatedMetadata = metadataCache.GetMetadata(relationship.RelatedModelType);
        
        if (relatedMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Related model {relationship.RelatedModelType.Name} must have a primary key for BelongsTo relationships");

        List<object> foreignKeyValues = models
            .Select(model => localForeignKeyProperty.PropertyInfo.GetValue(model))
            .Where(value => value != null)
            .Distinct()
            .ToList();

        if (foreignKeyValues.Count == 0) return;

        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(GetSelectExpressions(relatedMetadata))
            .From(relatedMetadata.TableName, relatedMetadata.SchemaName)
            .Where(relatedMetadata.PrimaryKey.ColumnName, "IN", foreignKeyValues.ToArray());

        if (relatedMetadata.HasSoftDelete && relatedMetadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(relatedMetadata, relatedMetadata.DeletedAtPropertyName);
            selectBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        
        MethodInfo loadMethod = GetType()
            .GetMethod(nameof(ExecuteRelatedQuery), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(relationship.RelatedModelType);
        
        List<object> relatedModels = await (Task<List<object>>)loadMethod.Invoke(this, new object[] { executor, selectBuilder })!;

        Dictionary<object, object> relatedModelsByPrimaryKey = relatedModels
            .ToDictionary(relatedModel => relatedMetadata.PrimaryKey.PropertyInfo.GetValue(relatedModel)!);

        foreach (TModel model in models)
        {
            object? foreignKeyValue = localForeignKeyProperty.PropertyInfo.GetValue(model);
            if (foreignKeyValue != null && relatedModelsByPrimaryKey.TryGetValue(foreignKeyValue, out object? relatedModel))
                relationship.PropertyInfo.SetValue(model, relatedModel);
        }
    }

    private string GetPropertyName(Expression<Func<TModel, object>> expression)
    {
        MemberExpression? memberExpression = expression.Body as MemberExpression;
        if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            memberExpression = unaryExpression.Operand as MemberExpression;

        if (memberExpression == null)
            throw new ArgumentException("Expression must be a member expression");

        return memberExpression.Member.Name;
    }

    private string GetColumnNameFromPropertyName(IModelMetadata metadata, string propertyName)
    {
        IPropertyMetadata? propertyMetadata = metadata.Properties.Values
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
} 