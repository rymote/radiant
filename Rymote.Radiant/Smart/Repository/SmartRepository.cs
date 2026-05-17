using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Smart.Context;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Executor;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Smart.Repository;

public sealed class SmartRepository<TModel> : ISmartRepository<TModel> where TModel : class, new()
{
    private readonly IDbConnection _databaseConnection;
    private readonly IModelMetadata _modelMetadata;
    private readonly SmartContext? _smartContext;

    public SmartRepository(IDbConnection databaseConnection, IModelMetadata modelMetadata)
        : this(databaseConnection, modelMetadata, smartContext: null)
    {
    }

    public SmartRepository(IDbConnection databaseConnection, IModelMetadata modelMetadata, SmartContext? smartContext)
    {
        _databaseConnection = databaseConnection;
        _modelMetadata = modelMetadata;
        _smartContext = smartContext;
    }

    private QueryExecutor CreateExecutor()
        => new QueryExecutor(_databaseConnection, _smartContext?.AmbientTransaction);

    private object? ApplyConverterToDatabase(object? value)
    {
        if (value is null || _smartContext is null) return value;
        if (_smartContext.Options.ValueConverters.TryGetValue(value.GetType(), out Rymote.Radiant.Smart.Configuration.ValueConverter? converter))
            return converter.ToDatabase(value);
        return value;
    }

    private object? ApplyConverterFromDatabase(object? databaseValue, Type clrPropertyType)
    {
        if (databaseValue is null || _smartContext is null) return databaseValue;
        if (_smartContext.Options.ValueConverters.TryGetValue(clrPropertyType, out Rymote.Radiant.Smart.Configuration.ValueConverter? converter))
            return converter.FromDatabase(databaseValue);
        return databaseValue;
    }

    private Rymote.Radiant.Sql.QueryCommand BuildCommand(InsertBuilder insertBuilder)
    {
        if (_smartContext is null)
            throw new InvalidOperationException(
                "SmartRepository requires a SmartContext to compile queries. Construct it with a SmartContext.");

        return Rymote.Radiant.Sql.Compiler.QueryCompiler.Compile(insertBuilder, _smartContext.Adapter);
    }

    private Rymote.Radiant.Sql.QueryCommand BuildCommand(UpdateBuilder updateBuilder)
    {
        if (_smartContext is null)
            throw new InvalidOperationException(
                "SmartRepository requires a SmartContext to compile queries. Construct it with a SmartContext.");

        return Rymote.Radiant.Sql.Compiler.QueryCompiler.Compile(updateBuilder, _smartContext.Adapter);
    }

    private Rymote.Radiant.Sql.QueryCommand BuildCommand(DeleteBuilder deleteBuilder)
    {
        if (_smartContext is null)
            throw new InvalidOperationException(
                "SmartRepository requires a SmartContext to compile queries. Construct it with a SmartContext.");

        return Rymote.Radiant.Sql.Compiler.QueryCompiler.Compile(deleteBuilder, _smartContext.Adapter);
    }

    public Task<TModel> InsertAsync(TModel model) => InsertAsync(model, CancellationToken.None);

    public async Task<TModel> InsertAsync(TModel model, CancellationToken cancellationToken)
    {
        InsertBuilder insertBuilder = new InsertBuilder()
            .Into(_modelMetadata.TableName, _modelMetadata.SchemaName);

        List<string> returningColumns = new List<string>();

        bool createdAtWasSet = false;
        bool updatedAtWasSet = false;

        foreach (IPropertyMetadata property in _modelMetadata.Properties.Values)
        {
            if (property.IsPrimaryKey && property.IsAutoIncrement)
            {
                returningColumns.Add(property.ColumnName);
                continue;
            }

            object? value = property.PropertyInfo.GetValue(model);

            if (_modelMetadata.HasTimestamps)
            {
                if (property.PropertyName == _modelMetadata.CreatedAtPropertyName && value != null)
                    createdAtWasSet = true;

                if (property.PropertyName == _modelMetadata.UpdatedAtPropertyName && value != null)
                    updatedAtWasSet = true;
            }

            if (value != null)
            {
                object convertedValue = ApplyConverterToDatabase(value) ?? value;
                ISqlExpression valueExpression = CreateValueExpression(convertedValue, property.DatabaseType);

                if (valueExpression is LiteralExpression && property.DatabaseType == null)
                    insertBuilder.Value(property.ColumnName, convertedValue);
                else
                    insertBuilder.ValueExpression(property.ColumnName, valueExpression);
            }
        }

        if (_modelMetadata.HasTimestamps)
        {
            DateTime now = DateTime.UtcNow;

            if (_modelMetadata.CreatedAtPropertyName != null && !createdAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(_modelMetadata.CreatedAtPropertyName), now);
                SetPropertyValue(model, _modelMetadata.CreatedAtPropertyName, now);
            }

            if (_modelMetadata.UpdatedAtPropertyName != null && !updatedAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(_modelMetadata.UpdatedAtPropertyName), now);
                SetPropertyValue(model, _modelMetadata.UpdatedAtPropertyName, now);
            }
        }

        if (returningColumns.Count > 0)
            insertBuilder.Returning(returningColumns.ToArray());

        QueryExecutor executor = CreateExecutor();

        if (returningColumns.Count > 0)
        {
            dynamic result = await executor.QuerySingleAsync<dynamic>(BuildCommand(insertBuilder), cancellationToken);

            if (_modelMetadata.PrimaryKey != null && _modelMetadata.PrimaryKey.IsAutoIncrement)
            {
                object primaryKeyDatabaseValue = ((IDictionary<string, object>)result)[_modelMetadata.PrimaryKey.ColumnName];
                object? primaryKeyClrValue = ApplyConverterFromDatabase(primaryKeyDatabaseValue, _modelMetadata.PrimaryKey.PropertyType)
                    ?? Convert.ChangeType(primaryKeyDatabaseValue, _modelMetadata.PrimaryKey.PropertyType);
                _modelMetadata.PrimaryKey.PropertyInfo.SetValue(model, primaryKeyClrValue);
            }
        }
        else
        {
            await executor.ExecuteAsync(BuildCommand(insertBuilder), cancellationToken);
        }

        return model;
    }

    public Task<TModel> UpdateAsync(TModel model) => UpdateAsync(model, CancellationToken.None);

    public async Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken)
    {
        if (_modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(_modelMetadata.TableName, _modelMetadata.SchemaName);

        bool updatedAtWasSet = false;

        foreach (IPropertyMetadata property in _modelMetadata.Properties.Values)
        {
            if (property.IsPrimaryKey)
                continue;

            if (_modelMetadata.HasTimestamps &&
                property.PropertyName == _modelMetadata.UpdatedAtPropertyName)
            {
                object? value = property.PropertyInfo.GetValue(model);
                if (value != null)
                {
                    updatedAtWasSet = true;
                    object convertedValue = ApplyConverterToDatabase(value) ?? value;
                    ISqlExpression valueExpression = CreateValueExpression(convertedValue, property.DatabaseType);

                    if (valueExpression is LiteralExpression && property.DatabaseType == null)
                        updateBuilder.Set(property.ColumnName, convertedValue);
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
                    object convertedValue = ApplyConverterToDatabase(propertyValue) ?? propertyValue;
                    ISqlExpression valueExpression = CreateValueExpression(convertedValue, property.DatabaseType);

                    if (valueExpression is LiteralExpression && property.DatabaseType == null)
                        updateBuilder.Set(property.ColumnName, convertedValue);
                    else
                        updateBuilder.SetExpression(property.ColumnName, valueExpression);
                }
            }
        }

        if (_modelMetadata.HasTimestamps &&
            _modelMetadata.UpdatedAtPropertyName != null &&
            !updatedAtWasSet)
        {
            DateTime now = DateTime.UtcNow;
            updateBuilder.Set(ConvertPropertyNameToColumnName(_modelMetadata.UpdatedAtPropertyName), now);
            SetPropertyValue(model, _modelMetadata.UpdatedAtPropertyName, now);
        }

        object? primaryKeyValue = _modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        object? primaryKeyForDatabase = ApplyConverterToDatabase(primaryKeyValue);
        updateBuilder.Where(_modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyForDatabase!);

        QueryExecutor executor = CreateExecutor();
        await executor.ExecuteAsync(BuildCommand(updateBuilder), cancellationToken);

        return model;
    }

    public Task<bool> DeleteAsync(TModel model) => DeleteAsync(model, CancellationToken.None);

    public async Task<bool> DeleteAsync(TModel model, CancellationToken cancellationToken)
    {
        if (_modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        DeleteBuilder deleteBuilder = new DeleteBuilder()
            .From(_modelMetadata.TableName, _modelMetadata.SchemaName);

        object? primaryKeyValue = _modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        object? primaryKeyForDatabase = ApplyConverterToDatabase(primaryKeyValue);
        deleteBuilder.Where(_modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyForDatabase!);

        QueryExecutor executor = CreateExecutor();
        int affectedRows = await executor.ExecuteAsync(BuildCommand(deleteBuilder), cancellationToken);

        return affectedRows > 0;
    }

    public Task<bool> SoftDeleteAsync(TModel model) => SoftDeleteAsync(model, CancellationToken.None);

    public async Task<bool> SoftDeleteAsync(TModel model, CancellationToken cancellationToken)
    {
        if (!_modelMetadata.HasSoftDelete || _modelMetadata.DeletedAtPropertyName == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");

        if (_modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(_modelMetadata.TableName, _modelMetadata.SchemaName);

        DateTime now = DateTime.UtcNow;
        updateBuilder.Set(ConvertPropertyNameToColumnName(_modelMetadata.DeletedAtPropertyName), now);

        object? primaryKeyValue = _modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        object? primaryKeyForDatabase = ApplyConverterToDatabase(primaryKeyValue);
        updateBuilder.Where(_modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyForDatabase!);

        QueryExecutor executor = CreateExecutor();
        int affectedRows = await executor.ExecuteAsync(BuildCommand(updateBuilder), cancellationToken);

        if (affectedRows > 0)
            SetPropertyValue(model, _modelMetadata.DeletedAtPropertyName, now);

        return affectedRows > 0;
    }

    public Task<bool> RestoreAsync(TModel model) => RestoreAsync(model, CancellationToken.None);

    public async Task<bool> RestoreAsync(TModel model, CancellationToken cancellationToken)
    {
        if (!_modelMetadata.HasSoftDelete || _modelMetadata.DeletedAtPropertyName == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not support soft delete");

        if (_modelMetadata.PrimaryKey == null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        UpdateBuilder updateBuilder = new UpdateBuilder()
            .Table(_modelMetadata.TableName, _modelMetadata.SchemaName);

        updateBuilder.Set(ConvertPropertyNameToColumnName(_modelMetadata.DeletedAtPropertyName), null);

        object? primaryKeyValue = _modelMetadata.PrimaryKey.PropertyInfo.GetValue(model);
        object? primaryKeyForDatabase = ApplyConverterToDatabase(primaryKeyValue);
        updateBuilder.Where(_modelMetadata.PrimaryKey.ColumnName, "=", primaryKeyForDatabase!);

        QueryExecutor executor = CreateExecutor();
        int affectedRows = await executor.ExecuteAsync(BuildCommand(updateBuilder), cancellationToken);

        if (affectedRows > 0)
            SetPropertyValue(model, _modelMetadata.DeletedAtPropertyName, null);

        return affectedRows > 0;
    }

    public async Task<TModel> UpsertAsync(TModel model, CancellationToken cancellationToken = default)
    {
        if (_modelMetadata.PrimaryKey is null)
            throw new InvalidOperationException($"Model {typeof(TModel).Name} does not have a primary key");

        bool adapterSupportsNativeUpsert = _smartContext is not null
            && _smartContext.Adapter.Capabilities.HasFlag(Rymote.Radiant.Adapters.DatabaseCapabilities.UpsertOnConflict);

        if (!adapterSupportsNativeUpsert)
            return await UpsertWithoutNativeOnConflictAsync(model, cancellationToken);

        return await UpsertWithNativeOnConflictAsync(model, cancellationToken);
    }

    private async Task<TModel> UpsertWithoutNativeOnConflictAsync(TModel model, CancellationToken cancellationToken)
    {
        object? primaryKeyValue = _modelMetadata.PrimaryKey!.PropertyInfo.GetValue(model);
        bool primaryKeyIsDefault = primaryKeyValue is null
            || (primaryKeyValue.GetType().IsValueType && primaryKeyValue.Equals(Activator.CreateInstance(primaryKeyValue.GetType())));

        return primaryKeyIsDefault
            ? await InsertAsync(model, cancellationToken)
            : await UpdateAsync(model, cancellationToken);
    }

    private async Task<TModel> UpsertWithNativeOnConflictAsync(TModel model, CancellationToken cancellationToken)
    {
        InsertBuilder insertBuilder = new InsertBuilder()
            .Into(_modelMetadata.TableName, _modelMetadata.SchemaName);

        List<string> columnsToUpdateFromExcluded = new List<string>();
        List<string> allColumnNames = new List<string>();
        bool createdAtWasSet = false;
        bool updatedAtWasSet = false;

        foreach (IPropertyMetadata property in _modelMetadata.Properties.Values)
        {
            allColumnNames.Add(property.ColumnName);

            object? value = property.PropertyInfo.GetValue(model);

            if (_modelMetadata.HasTimestamps)
            {
                if (property.PropertyName == _modelMetadata.CreatedAtPropertyName && value != null)
                    createdAtWasSet = true;
                if (property.PropertyName == _modelMetadata.UpdatedAtPropertyName && value != null)
                    updatedAtWasSet = true;
            }

            if (value is not null)
            {
                object convertedValue = ApplyConverterToDatabase(value) ?? value;
                ISqlExpression valueExpression = CreateValueExpression(convertedValue, property.DatabaseType);

                if (valueExpression is LiteralExpression && property.DatabaseType == null)
                    insertBuilder.Value(property.ColumnName, convertedValue);
                else
                    insertBuilder.ValueExpression(property.ColumnName, valueExpression);
            }

            if (!property.IsPrimaryKey)
                columnsToUpdateFromExcluded.Add(property.ColumnName);
        }

        if (_modelMetadata.HasTimestamps)
        {
            DateTime now = DateTime.UtcNow;
            if (_modelMetadata.CreatedAtPropertyName != null && !createdAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(_modelMetadata.CreatedAtPropertyName), now);
                SetPropertyValue(model, _modelMetadata.CreatedAtPropertyName, now);
            }
            if (_modelMetadata.UpdatedAtPropertyName != null && !updatedAtWasSet)
            {
                insertBuilder.Value(ConvertPropertyNameToColumnName(_modelMetadata.UpdatedAtPropertyName), now);
                SetPropertyValue(model, _modelMetadata.UpdatedAtPropertyName, now);
            }
        }

        insertBuilder.OnConflictDoUpdateFromExcluded(
            conflictColumns: new[] { _modelMetadata.PrimaryKey!.ColumnName },
            columnsToUpdateFromExcluded: columnsToUpdateFromExcluded.ToArray());

        insertBuilder.Returning(allColumnNames.ToArray());

        QueryExecutor executor = CreateExecutor();
        TModel hydratedModel = await executor.QuerySingleAsync<TModel>(BuildCommand(insertBuilder), cancellationToken);
        return hydratedModel;
    }

    public Task<bool> ForceDeleteAsync(TModel model, CancellationToken cancellationToken = default)
        => DeleteAsync(model, cancellationToken);

    public async Task<IReadOnlyList<TModel>> InsertManyAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
    {
        List<TModel> materialised = models.ToList();
        if (materialised.Count == 0) return materialised;

        foreach (TModel model in materialised)
            await InsertAsync(model, cancellationToken);

        return materialised;
    }

    private void SetPropertyValue(TModel model, string propertyName, object? value)
    {
        IPropertyMetadata? property = _modelMetadata.Properties.Values
            .FirstOrDefault(prop => prop.PropertyName == propertyName);

        if (property != null)
            property.PropertyInfo.SetValue(model, value);
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