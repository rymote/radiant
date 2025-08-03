using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart.Repository;

public sealed class SmartRepository<TModel> : ISmartRepository<TModel> where TModel : class, new()
{
    private readonly IDbConnection databaseConnection;
    private readonly IModelMetadata modelMetadata;

    public SmartRepository(IDbConnection databaseConnection, IModelMetadata modelMetadata)
    {
        this.databaseConnection = databaseConnection;
        this.modelMetadata = modelMetadata;
    }

    public async Task<TModel> InsertAsync(TModel model)
    {
        InsertBuilder insertBuilder = new InsertBuilder()
            .Into(modelMetadata.TableName, modelMetadata.SchemaName);

        List<string> returningColumns = new List<string>();

        bool createdAtWasSet = false;
        bool updatedAtWasSet = false;

        foreach (IPropertyMetadata property in modelMetadata.Properties.Values)
        {
            if (property.IsPrimaryKey && property.IsAutoIncrement)
            {
                returningColumns.Add(property.ColumnName);
                continue;
            }

            object? value = property.PropertyInfo.GetValue(model);

            if (modelMetadata.HasTimestamps)
            {
                if (property.PropertyName == modelMetadata.CreatedAtPropertyName && value != null)
                    createdAtWasSet = true;
                if (property.PropertyName == modelMetadata.UpdatedAtPropertyName && value != null)
                    updatedAtWasSet = true;
            }

            if (value != null)
            {
                ISqlExpression valueExpression = CreateValueExpression(value, property.DatabaseType);

                if (valueExpression is LiteralExpression && property.DatabaseType == null)
                    insertBuilder.Value(property.ColumnName, value);
                else
                    insertBuilder.ValueExpression(property.ColumnName, valueExpression);
            }
        }

        if (modelMetadata.HasTimestamps)
        {
            DateTime now = DateTime.UtcNow;

            if (modelMetadata.CreatedAtPropertyName != null && !createdAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(modelMetadata.CreatedAtPropertyName), now);
                SetPropertyValue(model, modelMetadata.CreatedAtPropertyName, now);
            }

            if (modelMetadata.UpdatedAtPropertyName != null && !updatedAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(modelMetadata.UpdatedAtPropertyName), now);
                SetPropertyValue(model, modelMetadata.UpdatedAtPropertyName, now);
            }
        }

        if (returningColumns.Count > 0)
            insertBuilder.Returning(returningColumns.ToArray());

        QueryExecutor executor = new QueryExecutor(databaseConnection);

        if (returningColumns.Count > 0)
        {
            dynamic result = await executor.QuerySingleAsync<dynamic>(insertBuilder.Build());

            if (modelMetadata.PrimaryKey != null && modelMetadata.PrimaryKey.IsAutoIncrement)
            {
                object primaryKeyValue = ((IDictionary<string, object>)result)[modelMetadata.PrimaryKey.ColumnName];
                modelMetadata.PrimaryKey.PropertyInfo.SetValue(model,
                    Convert.ChangeType(primaryKeyValue, modelMetadata.PrimaryKey.PropertyType));
            }
        }
        else
        {
            await executor.ExecuteAsync(insertBuilder.Build());
        }

        return model;
    }

    public async Task<TModel> UpdateAsync(TModel model)
    {
        if (modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(modelMetadata.TableName, modelMetadata.SchemaName);

        bool updatedAtWasSet = false;

        foreach (IPropertyMetadata property in modelMetadata.Properties.Values)
        {
            if (property.IsPrimaryKey)
                continue;

            if (modelMetadata.HasTimestamps &&
                property.PropertyName == modelMetadata.UpdatedAtPropertyName)
            {
                object? value = property.PropertyInfo.GetValue(model);
                if (value != null)
                {
                    updatedAtWasSet = true;
                    ISqlExpression valueExpression = CreateValueExpression(value, property.DatabaseType);

                    if (valueExpression is LiteralExpression && property.DatabaseType == null)
                        updateBuilder.Set(property.ColumnName, value);
                    else
                        updateBuilder.SetExpression(property.ColumnName, valueExpression);
                }

                continue;
            }

            object? propertyValue = property.PropertyInfo.GetValue(model);
            if (propertyValue != null || property.IsNullable)
            {
                if (propertyValue == null)
                {
                    updateBuilder.Set(property.ColumnName, null);
                }
                else
                {
                    ISqlExpression valueExpression = CreateValueExpression(propertyValue, property.DatabaseType);

                    if (valueExpression is LiteralExpression && property.DatabaseType == null)
                        updateBuilder.Set(property.ColumnName, propertyValue);
                    else
                        updateBuilder.SetExpression(property.ColumnName, valueExpression);
                }
            }
        }

        if (modelMetadata.HasTimestamps &&
            modelMetadata.UpdatedAtPropertyName != null &&
            !updatedAtWasSet)
        {
            DateTime now = DateTime.UtcNow;
            updateBuilder.Set(ConvertPropertyNameToColumnName(modelMetadata.UpdatedAtPropertyName), now);
            SetPropertyValue(model, modelMetadata.UpdatedAtPropertyName, now);
        }

        object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        updateBuilder.Where(modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyValue!);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        await executor.ExecuteAsync(updateBuilder.Build());

        return model;
    }

    public async Task<bool> DeleteAsync(TModel model)
    {
        if (modelMetadata.PrimaryKey == null)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");
        }

        DeleteBuilder deleteBuilder = new DeleteBuilder()
            .From(modelMetadata.TableName, modelMetadata.SchemaName);

        object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        deleteBuilder.Where(modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyValue!);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        int affectedRows = await executor.ExecuteAsync(deleteBuilder.Build());

        return affectedRows > 0;
    }

    public async Task<bool> SoftDeleteAsync(TModel model)
    {
        if (!modelMetadata.HasSoftDelete || modelMetadata.DeletedAtPropertyName == null)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");
        }

        if (modelMetadata.PrimaryKey == null)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");
        }

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(modelMetadata.TableName, modelMetadata.SchemaName);

        DateTime now = DateTime.UtcNow;
        updateBuilder.Set(ConvertPropertyNameToColumnName(modelMetadata.DeletedAtPropertyName), now);

        object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        updateBuilder.Where(modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyValue!);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        int affectedRows = await executor.ExecuteAsync(updateBuilder.Build());

        if (affectedRows > 0)
        {
            SetPropertyValue(model, modelMetadata.DeletedAtPropertyName, now);
        }

        return affectedRows > 0;
    }

    public async Task<bool> RestoreAsync(TModel model)
    {
        if (!modelMetadata.HasSoftDelete || modelMetadata.DeletedAtPropertyName == null)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");
        }

        if (modelMetadata.PrimaryKey == null)
        {
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");
        }

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(modelMetadata.TableName, modelMetadata.SchemaName);

        updateBuilder.Set(ConvertPropertyNameToColumnName(modelMetadata.DeletedAtPropertyName), null);

        object? primaryKeyValue = modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        updateBuilder.Where(modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyValue!);

        QueryExecutor executor = new QueryExecutor(databaseConnection);
        int affectedRows = await executor.ExecuteAsync(updateBuilder.Build());

        if (affectedRows > 0)
        {
            SetPropertyValue(model, modelMetadata.DeletedAtPropertyName, null);
        }

        return affectedRows > 0;
    }

    private void SetPropertyValue(TModel model, string propertyName, object? value)
    {
        IPropertyMetadata? property = modelMetadata.Properties.Values
            .FirstOrDefault(prop => prop.PropertyName == propertyName);

        if (property != null)
        {
            property.PropertyInfo.SetValue(model, value);
        }
    }

    private string ConvertPropertyNameToColumnName(string propertyName)
    {
        return string.Concat(propertyName.Select((character, index) =>
                index > 0 && char.IsUpper(character) ? "_" + character : character.ToString()))
            .ToLower();
    }

    private ISqlExpression CreateValueExpression(object value, string? databaseType)
    {
        if (string.IsNullOrEmpty(databaseType))
            return new LiteralExpression(value);

        switch (databaseType.ToLowerInvariant())
        {
            case "jsonb":
            case "json":
                return new CastExpression(new LiteralExpression(value), databaseType);

            case "uuid":
                return new CastExpression(new LiteralExpression(value.ToString()), "uuid");

            case "inet":
            case "cidr":
            case "macaddr":
            case "macaddr8":
                return new CastExpression(new LiteralExpression(value.ToString()), databaseType);

            case "point":
            case "line":
            case "lseg":
            case "box":
            case "path":
            case "polygon":
            case "circle":
                return new CastExpression(new LiteralExpression(value.ToString()), databaseType);

            case "int[]":
            case "text[]":
            case "varchar[]":
            case "bigint[]":
            case "smallint[]":
            case "boolean[]":
            case "real[]":
            case "double precision[]":
            case "numeric[]":
            case "decimal[]":
                if (value is Array array)
                    return new CastExpression(new ArrayLiteralExpression(array as object[]), databaseType);

                return new CastExpression(new LiteralExpression(value), databaseType);

            case "tsquery":
            case "tsvector":
                return new CastExpression(new LiteralExpression(value.ToString()), databaseType);

            case "interval":
            case "daterange":
            case "tsrange":
            case "tstzrange":
            case "numrange":
            case "int4range":
            case "int8range":
                return new CastExpression(new LiteralExpression(value.ToString()), databaseType);

            case "hstore":
                return new CastExpression(new LiteralExpression(value.ToString()), "hstore");

            case "xml":
                return new CastExpression(new LiteralExpression(value.ToString()), "xml");

            case "bytea":
                if (value is byte[] bytes)
                    return new CastExpression(new LiteralExpression(Convert.ToBase64String(bytes)), "bytea");

                return new CastExpression(new LiteralExpression(value), "bytea");

            case "vector":
                if (value is float[] floatArray)
                {
                    string vectorString = $"[{string.Join(",", floatArray)}]";
                    return new CastExpression(new LiteralExpression(vectorString), "vector");
                }

                return new CastExpression(new LiteralExpression(value.ToString()), "vector");

            default:
                if (databaseType.Contains("[]") || 
                    databaseType.StartsWith("pg_") || 
                    databaseType.Contains(".")) 
                {
                    return new CastExpression(new LiteralExpression(value), databaseType);
                }

                return new LiteralExpression(value);
        }
    }
}