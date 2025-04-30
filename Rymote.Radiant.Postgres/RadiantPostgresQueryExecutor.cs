using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Dapper;
using Rymote.Radiant.Core;

namespace Rymote.Radiant.Postgres;

public sealed class RadiantPostgresQueryExecutor(IDbConnection connection) : RadiantQueryExecutorBase
{
    private readonly IDbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly Dictionary<Type, Dictionary<string, string>> _mappings = new();

    public override IRadiantQueryExecutor MapProperty<TPoco>(string propertyName, string columnName)
    {
        Type pocoType = typeof(TPoco);
        if (!_mappings.ContainsKey(pocoType))
            _mappings[pocoType] = new Dictionary<string, string>();

        _mappings[pocoType][propertyName] = columnName;
        return this;
    }
    
    private string GetMappedColumnFor<T>(string propertyName)
    {
        Type pocoType = typeof(T);
        return _mappings.TryGetValue(pocoType, out var map) && map.TryGetValue(propertyName, out string? mapped)
            ? mapped
            : propertyName;
    }
    
    protected override IEnumerable<T> ExecuteCore<T>(RadiantQueryData data)
    {
        if (string.IsNullOrWhiteSpace(data.TargetName))
            throw new InvalidOperationException("No target (table) specified for Postgres.");

        return data.OperationType switch
        {
            RadiantOperationType.Create => ExecuteInsert<T>(data),
            RadiantOperationType.Get    => ExecuteSelect<T>(data),
            RadiantOperationType.Update => ExecuteUpdate<T>(data),
            RadiantOperationType.Remove => ExecuteDelete<T>(data),
            _ => throw new InvalidOperationException($"Unsupported operation: {data.OperationType}")
        };
    }

    private static string QuoteIdentifierIfNeeded(string identifier)
    {
        if (identifier.Length >= 2 && identifier.StartsWith("\"") && identifier.EndsWith("\""))
            return identifier;

        bool hasUpper = identifier.Any(char.IsUpper);
        
        return hasUpper ? $"\"{identifier}\"" : identifier;
    }

    private string GetSelectColumn<T>(string propertyName)
    {
        string mapped = GetMappedColumnFor<T>(propertyName);
        Console.WriteLine("{0} {1}", mapped, propertyName);
        
        return mapped != propertyName ? 
            $"{QuoteIdentifierIfNeeded(mapped)} AS {QuoteIdentifierIfNeeded(propertyName)}" : 
            QuoteIdentifierIfNeeded(propertyName);
    }

    private static string BuildFullTableName(RadiantQueryData data)
    {
        string table = data.TargetName!.Trim();
        string quotedTable = QuoteIdentifierIfNeeded(table);

        if (string.IsNullOrWhiteSpace(data.Schema)) 
            return quotedTable;
        
        string schema = data.Schema.Trim();
        string quotedSchema = QuoteIdentifierIfNeeded(schema);
        return $"{quotedSchema}.{quotedTable}";
    }

    private IEnumerable<T> ExecuteInsert<T>(RadiantQueryData data)
    {
        if (!data.CreateValues.Any())
            throw new InvalidOperationException("No CreateValues for insertion.");

        string tableName = BuildFullTableName(data);

        string[] columns = data.CreateValues.Keys
            .Select(key => QuoteIdentifierIfNeeded(GetMappedColumnFor<T>(key)))
            .ToArray();

        string columnList = string.Join(", ", columns);
        string parameterList = string.Join(", ", data.CreateValues.Keys.Select(k => $"@{k}"));

        string sql = new StringBuilder()
            .Append($"INSERT INTO {tableName} ({columnList}) ")
            .Append($"VALUES ({parameterList}) RETURNING *;")
            .ToString();

        Dictionary<string, object?> parameterDictionary = new Dictionary<string, object?>(data.CreateValues);
        return _connection.Query<T>(sql, parameterDictionary);
    }

    private IEnumerable<T> ExecuteSelect<T>(RadiantQueryData data)
    {
        string tableName = BuildFullTableName(data);

        string columns;
        if (data.Columns.Count != 0 && data.Columns[0].Trim() != "*")
        {
            data.Columns.ForEach(Console.WriteLine);
            columns = string.Join(", ", data.Columns.Select(GetSelectColumn<T>));
        }
        else
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            columns = string.Join(", ", properties.Select(property => GetSelectColumn<T>(property.Name)));
        }

        StringBuilder stringBuilder = new StringBuilder()
            .Append("SELECT ").Append(columns)
            .Append(" FROM ").Append(tableName);

        if (data.WhereClauses.Count != 0)
            stringBuilder.Append(" WHERE ").Append(string.Join(" AND ", data.WhereClauses));

        if (data.Limit.HasValue)
            stringBuilder.Append(" LIMIT ").Append(data.Limit.Value);
        
        if (data.Offset.HasValue)
            stringBuilder.Append(" OFFSET ").Append(data.Offset.Value);

        stringBuilder.Append(";");

        Console.WriteLine(stringBuilder.ToString());
        
        return _connection.Query<T>(stringBuilder.ToString());
    }

    private IEnumerable<T> ExecuteUpdate<T>(RadiantQueryData data)
    {
        if (!data.UpdateValues.Any())
            throw new InvalidOperationException("No UpdateValues for update.");

        string tableName = BuildFullTableName(data);

        string[] setClauses = data.UpdateValues
            .Select(kvp => $"{QuoteIdentifierIfNeeded(GetMappedColumnFor<T>(kvp.Key))} = @{kvp.Key}")
            .ToArray();

        StringBuilder stringBuilder = new StringBuilder()
            .Append("UPDATE ").Append(tableName)
            .Append(" SET ")
            .Append(string.Join(", ", setClauses));

        if (data.WhereClauses.Count != 0)
            stringBuilder.Append(" WHERE ").Append(string.Join(" AND ", data.WhereClauses));

        stringBuilder.Append(" RETURNING *;");

        Dictionary<string, object?> parameterDictionary = new Dictionary<string, object?>(data.UpdateValues);
        return _connection.Query<T>(stringBuilder.ToString(), parameterDictionary);
    }

    private IEnumerable<T> ExecuteDelete<T>(RadiantQueryData data)
    {
        string tableName = BuildFullTableName(data);

        StringBuilder stringBuilder = new StringBuilder()
            .Append("DELETE FROM ").Append(tableName);

        if (data.WhereClauses.Count != 0)
            stringBuilder.Append(" WHERE ").Append(string.Join(" AND ", data.WhereClauses));

        stringBuilder.Append(";");

        int affected = _connection.Execute(stringBuilder.ToString());

        if (typeof(T) != typeof(int)) 
            return Array.Empty<T>();
        
        List<int> list = new List<int> { affected };
        return (IEnumerable<T>) (object) list;
    }
}