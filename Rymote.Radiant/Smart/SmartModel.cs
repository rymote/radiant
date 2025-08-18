using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Rymote.Radiant.Smart.Connection;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Smart.Query;
using Rymote.Radiant.Smart.Repository;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart;

public abstract class SmartModel
{
    private static IConnectionResolver? _connectionResolver;
    private static IModelMetadataCache? _modelMetadataCache;

    public static void Configure(IDbConnection connection, IModelMetadataCache cache)
    {
        _connectionResolver = new StaticConnectionResolver(connection);
        _modelMetadataCache = cache;
    }

    public static void Configure(IConnectionResolver resolver, IModelMetadataCache cache)
    {
        _connectionResolver = resolver;
        _modelMetadataCache = cache;
    }
    
    protected static IDbConnection GetConnection()
    {
        if (_connectionResolver == null)
            throw new InvalidOperationException("SmartModel has not been configured. Call SmartModel.Configure() first.");
        
        return _connectionResolver.GetConnection();
    }

    public static IModelMetadataCache GetMetadataCache()
    {
        if (_modelMetadataCache == null)
            throw new InvalidOperationException("SmartModel has not been configured. Call SmartModel.Configure() first.");
            
        return _modelMetadataCache;
    }
}

public abstract class SmartModel<TModel> : SmartModel where TModel : SmartModel<TModel>, new()
{
    public static ISmartQuery<TModel> Query()
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        return new SmartQuery<TModel>(connection, metadata);
    }

    public static ISmartRawQuery Raw()
    {
        IDbConnection connection = GetConnection();
        return new SmartRawQuery(connection);
    }

    public static async Task<List<TModel>> FromSqlAsync(string sql, object? parameters = null)
    {
        IDbConnection connection = GetConnection();
        IEnumerable<TModel> results = await connection.QueryAsync<TModel>(sql, parameters);
        return results.ToList();
    }

    public static async Task<TModel?> FromSqlSingleAsync(string sql, object? parameters = null)
    {
        IDbConnection connection = GetConnection();
        return await connection.QuerySingleOrDefaultAsync<TModel?>(sql, parameters);
    }

    public static async Task<TModel?> FindAsync(object primaryKeyValue)
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        
        if (metadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From(metadata.TableName, metadata.SchemaName)
            .Where(metadata.PrimaryKey.ColumnName, "=", primaryKeyValue);

        if (metadata.HasSoftDelete && metadata.DeletedAtPropertyName != null)
        {
            string deletedAtColumnName = GetColumnNameFromPropertyName(metadata, metadata.DeletedAtPropertyName);
            selectBuilder.WhereNull(deletedAtColumnName);
        }

        QueryExecutor executor = new QueryExecutor(connection);
        IEnumerable<TModel> results = await executor.QueryAsync<TModel>(selectBuilder.Build());
        return results.FirstOrDefault();
    }

    public static async Task<List<TModel>> AllAsync()
    {
        return await Query().ToListAsync();
    }

    public static async Task<TModel> CreateAsync(TModel model)
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        ISmartRepository<TModel> repository = new SmartRepository<TModel>(connection, metadata);
        return await repository.InsertAsync(model);
    }

    public async Task<TModel> SaveAsync()
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        ISmartRepository<TModel> repository = new SmartRepository<TModel>(connection, metadata);
        
        object? primaryKeyValue = GetPrimaryKeyValue(metadata);
        
        if (primaryKeyValue == null || IsDefaultValue(primaryKeyValue))
            return await repository.InsertAsync((TModel)this);
        else
            return await repository.UpdateAsync((TModel)this);
    }

    public async Task<bool> DeleteAsync()
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        ISmartRepository<TModel> repository = new SmartRepository<TModel>(connection, metadata);
        
        if (metadata.HasSoftDelete)
            return await repository.SoftDeleteAsync((TModel)this);
        else
            return await repository.DeleteAsync((TModel)this);
    }

    public async Task<bool> RestoreAsync()
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        
        if (!metadata.HasSoftDelete)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");
        
        ISmartRepository<TModel> repository = new SmartRepository<TModel>(connection, metadata);
        return await repository.RestoreAsync((TModel)this);
    }

    public async Task<bool> ForceDeleteAsync()
    {
        IDbConnection connection = GetConnection();
        IModelMetadata metadata = GetMetadataCache().GetMetadata<TModel>();
        ISmartRepository<TModel> repository = new SmartRepository<TModel>(connection, metadata);
        return await repository.DeleteAsync((TModel)this);
    }

    private object? GetPrimaryKeyValue(IModelMetadata metadata)
    {
        if (metadata.PrimaryKey == null)
            return null;

        return metadata.PrimaryKey.PropertyInfo.GetValue(this);
    }

    private bool IsDefaultValue(object value)
    {
        Type type = value.GetType();
        
        if (type.IsValueType)
        {
            object? defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }
        
        return value == null;
    }

    private static string GetColumnNameFromPropertyName(IModelMetadata metadata, string propertyName)
    {
        IPropertyMetadata? propertyMetadata = metadata.Properties.Values
            .FirstOrDefault(property => property.PropertyName == propertyName);

        if (propertyMetadata != null)
            return propertyMetadata.ColumnName;

        return ConvertPropertyNameToColumnName(propertyName);
    }

    private static string ConvertPropertyNameToColumnName(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) => 
            index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }
}