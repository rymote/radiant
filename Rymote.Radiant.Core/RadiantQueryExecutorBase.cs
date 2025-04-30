namespace Rymote.Radiant.Core;

public abstract class RadiantQueryExecutorBase : IRadiantQueryExecutor
{
    protected RadiantQueryData _data = new RadiantQueryData();

    public virtual IRadiantQueryExecutor Create()
    {
        _data.OperationType = RadiantOperationType.Create;
        return this;
    }

    public virtual IRadiantQueryExecutor Get(params string[] columns)
    {
        _data.OperationType = RadiantOperationType.Get;
        if (columns is { Length: > 0 })
            _data.Columns.AddRange(columns);
        
        return this;
    }

    public virtual IRadiantQueryExecutor Update()
    {
        _data.OperationType = RadiantOperationType.Update;
        return this;
    }

    public virtual IRadiantQueryExecutor Remove()
    {
        _data.OperationType = RadiantOperationType.Remove;
        return this;
    }

    public virtual IRadiantQueryExecutor Schema(string schemaName)
    {
        _data.Schema = schemaName;
        return this;
    }

    public virtual IRadiantQueryExecutor Target(string tableOrCollection)
    {
        _data.TargetName = tableOrCollection;
        return this;
    }

    public virtual IRadiantQueryExecutor Where(string condition)
    {
        _data.WhereClauses.Add(condition);
        return this;
    }

    public virtual IRadiantQueryExecutor CreateValue(string field, object? value)
    {
        _data.CreateValues[field] = value;
        return this;
    }

    public virtual IRadiantQueryExecutor UpdateValue(string field, object? value)
    {
        _data.UpdateValues[field] = value;
        return this;
    }

    public virtual IRadiantQueryExecutor Limit(int limit)
    {
        _data.Limit = limit;
        return this;
    }

    public virtual IRadiantQueryExecutor Offset(int offset)
    {
        _data.Offset = offset;
        return this;
    }

    public abstract IRadiantQueryExecutor MapProperty<TPoco>(string propertyName, string columnName);
    
    public IEnumerable<T> Execute<T>() => ExecuteCore<T>(_data);

    protected abstract IEnumerable<T> ExecuteCore<T>(RadiantQueryData data);
}