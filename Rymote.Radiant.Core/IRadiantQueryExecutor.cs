namespace Rymote.Radiant.Core;

public interface IRadiantQueryExecutor
{
    IRadiantQueryExecutor Create();
    IRadiantQueryExecutor Get(params string[] columns);
    IRadiantQueryExecutor Update();
    IRadiantQueryExecutor Remove();

    IRadiantQueryExecutor Schema(string schemaName);

    IRadiantQueryExecutor Target(string tableOrCollection);
    IRadiantQueryExecutor Where(string condition);

    IRadiantQueryExecutor CreateValue(string field, object? value);
    IRadiantQueryExecutor UpdateValue(string field, object? value);

    IRadiantQueryExecutor Limit(int limit);
    IRadiantQueryExecutor Offset(int offset);

    IRadiantQueryExecutor MapProperty<TPoco>(string propertyName, string columnName);
    
    IEnumerable<T> Execute<T>();
}