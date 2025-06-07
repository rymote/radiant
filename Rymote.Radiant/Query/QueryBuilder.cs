using System.Data;
using System.Text;
using Dapper;

namespace Rymote.Radiant.Query;

public class QueryBuilder<TModel> where TModel : class
{
    private readonly IDbConnection connection;
    private readonly StringBuilder queryBuilder;
    private readonly DynamicParameters parameters;
    private readonly string tableName;
    private readonly string schema;
    private readonly List<string> selectColumns;
    private readonly List<string> whereConditions;
    private readonly List<string> joinClauses;
    private readonly List<string> groupByColumns;
    private readonly List<string> havingConditions;
    private readonly List<string> orderByColumns;
    private int? limitValue;
    private int? offsetValue;

    public QueryBuilder(IDbConnection connection, string tableName, string schema = "public")
    {
        this.connection = connection;
        this.tableName = tableName;
        this.schema = schema;
        queryBuilder = new StringBuilder();
        parameters = new DynamicParameters();
        selectColumns = new List<string>();
        whereConditions = new List<string>();
        joinClauses = new List<string>();
        groupByColumns = new List<string>();
        havingConditions = new List<string>();
        orderByColumns = new List<string>();
    }

    public QueryBuilder<TModel> Select(params string[] columns)
    {
        selectColumns.AddRange(columns);
        return this;
    }

    public QueryBuilder<TModel> SelectRaw(string rawSql)
    {
        selectColumns.Add(rawSql);
        return this;
    }

    public QueryBuilder<TModel> Where(string column, string operatorValue, object value)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} {operatorValue} @{parameterName}");
        parameters.Add(parameterName, value);
        return this;
    }

    public QueryBuilder<TModel> Where(string condition, object? parameters = null)
    {
        whereConditions.Add(condition);
        if (parameters != null)
        {
            this.parameters.AddDynamicParams(parameters);
        }
        return this;
    }

    public QueryBuilder<TModel> WhereIn<T>(string column, IEnumerable<T> values)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} = ANY(@{parameterName})");
        parameters.Add(parameterName, values.ToArray());
        return this;
    }

    public QueryBuilder<TModel> WhereNotIn<T>(string column, IEnumerable<T> values)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} != ALL(@{parameterName})");
        parameters.Add(parameterName, values.ToArray());
        return this;
    }

    public QueryBuilder<TModel> WhereBetween<T>(string column, T start, T end)
    {
        string startParam = $"start_{parameters.ParameterNames.Count()}";
        string endParam = $"end_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} BETWEEN @{startParam} AND @{endParam}");
        parameters.Add(startParam, start);
        parameters.Add(endParam, end);
        return this;
    }

    public QueryBuilder<TModel> WhereNull(string column)
    {
        whereConditions.Add($"{column} IS NULL");
        return this;
    }

    public QueryBuilder<TModel> WhereNotNull(string column)
    {
        whereConditions.Add($"{column} IS NOT NULL");
        return this;
    }

    public QueryBuilder<TModel> WhereLike(string column, string pattern)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} ILIKE @{parameterName}");
        parameters.Add(parameterName, pattern);
        return this;
    }

    public QueryBuilder<TModel> WhereJsonContains(string column, object value)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column} @> @{parameterName}::jsonb");
        parameters.Add(parameterName, System.Text.Json.JsonSerializer.Serialize(value));
        return this;
    }

    public QueryBuilder<TModel> WhereJsonPath(string column, string path, object value)
    {
        string parameterName = $"param_{parameters.ParameterNames.Count()}";
        whereConditions.Add($"{column}->'{path}' = @{parameterName}::jsonb");
        parameters.Add(parameterName, System.Text.Json.JsonSerializer.Serialize(value));
        return this;
    }

    public QueryBuilder<TModel> WhereExists(QueryBuilder<TModel> subQuery)
    {
        whereConditions.Add($"EXISTS ({subQuery.ToSql()})");
        parameters.AddDynamicParams(subQuery.parameters);
        return this;
    }

    public QueryBuilder<TModel> Join(string table, string condition, string type = "INNER")
    {
        joinClauses.Add($"{type} JOIN {table} ON {condition}");
        return this;
    }

    public QueryBuilder<TModel> LeftJoin(string table, string condition)
    {
        return Join(table, condition, "LEFT");
    }

    public QueryBuilder<TModel> RightJoin(string table, string condition)
    {
        return Join(table, condition, "RIGHT");
    }

    public QueryBuilder<TModel> InnerJoin(string table, string condition)
    {
        return Join(table, condition, "INNER");
    }

    public QueryBuilder<TModel> FullJoin(string table, string condition)
    {
        return Join(table, condition, "FULL");
    }

    public QueryBuilder<TModel> GroupBy(params string[] columns)
    {
        groupByColumns.AddRange(columns);
        return this;
    }

    public QueryBuilder<TModel> Having(string condition, object? parameters = null)
    {
        havingConditions.Add(condition);
        if (parameters != null)
        {
            this.parameters.AddDynamicParams(parameters);
        }
        return this;
    }

    public QueryBuilder<TModel> OrderBy(string column, string direction = "ASC")
    {
        orderByColumns.Add($"{column} {direction}");
        return this;
    }

    public QueryBuilder<TModel> OrderByDesc(string column)
    {
        return OrderBy(column, "DESC");
    }

    public QueryBuilder<TModel> OrderByRaw(string orderExpression)
    {
        orderByColumns.Add(orderExpression);
        return this;
    }

    public QueryBuilder<TModel> Limit(int count)
    {
        limitValue = count;
        return this;
    }

    public QueryBuilder<TModel> Offset(int count)
    {
        offsetValue = count;
        return this;
    }

    public QueryBuilder<TModel> Skip(int count)
    {
        return Offset(count);
    }

    public QueryBuilder<TModel> Take(int count)
    {
        return Limit(count);
    }

    public QueryBuilder<TModel> Clone()
    {
        var clone = new QueryBuilder<TModel>(connection, tableName, schema);
        clone.selectColumns.AddRange(selectColumns);
        clone.whereConditions.AddRange(whereConditions);
        clone.joinClauses.AddRange(joinClauses);
        clone.groupByColumns.AddRange(groupByColumns);
        clone.havingConditions.AddRange(havingConditions);
        clone.orderByColumns.AddRange(orderByColumns);
        clone.limitValue = limitValue;
        clone.offsetValue = offsetValue;
        
        // Clone parameters
        foreach (var paramName in parameters.ParameterNames)
        {
            clone.parameters.Add(paramName, parameters.Get<object>(paramName));
        }
        
        return clone;
    }

    public string ToSql()
    {
        StringBuilder sql = new StringBuilder();

        // SELECT clause
        sql.Append("SELECT ");
        if (selectColumns.Any())
        {
            sql.Append(string.Join(", ", selectColumns));
        }
        else
        {
            sql.Append("*");
        }

        // FROM clause
        sql.Append($" FROM \"{schema}\".\"{tableName}\"");

        // JOIN clauses
        if (joinClauses.Any())
        {
            sql.Append(" ");
            sql.Append(string.Join(" ", joinClauses));
        }

        // WHERE clause
        if (whereConditions.Any())
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", whereConditions));
        }

        // GROUP BY clause
        if (groupByColumns.Any())
        {
            sql.Append(" GROUP BY ");
            sql.Append(string.Join(", ", groupByColumns));
        }

        // HAVING clause
        if (havingConditions.Any())
        {
            sql.Append(" HAVING ");
            sql.Append(string.Join(" AND ", havingConditions));
        }

        // ORDER BY clause
        if (orderByColumns.Any())
        {
            sql.Append(" ORDER BY ");
            sql.Append(string.Join(", ", orderByColumns));
        }

        // LIMIT clause
        if (limitValue.HasValue)
        {
            sql.Append($" LIMIT {limitValue.Value}");
        }

        // OFFSET clause
        if (offsetValue.HasValue)
        {
            sql.Append($" OFFSET {offsetValue.Value}");
        }

        return sql.ToString();
    }

    public async Task<IEnumerable<TModel>> GetAsync()
    {
        string sql = ToSql();
        return await connection.QueryAsync<TModel>(sql, parameters);
    }

    public async Task<TModel> FirstAsync()
    {
        Limit(1);
        string sql = ToSql();
        return await connection.QueryFirstAsync<TModel>(sql, parameters);
    }

    public async Task<TModel?> FirstOrDefaultAsync()
    {
        Limit(1);
        string sql = ToSql();
        return await connection.QueryFirstOrDefaultAsync<TModel>(sql, parameters);
    }

    public async Task<int> CountAsync()
    {
        selectColumns.Clear();
        selectColumns.Add("COUNT(*)");
        string sql = ToSql();
        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<bool> ExistsAsync()
    {
        return await CountAsync() > 0;
    }

    public async Task<int> UpdateAsync(object updateValues)
    {
        StringBuilder sql = new StringBuilder();
        sql.Append($"UPDATE \"{schema}\".\"{tableName}\" SET ");

        List<string> setClauses = new List<string>();
        foreach (System.Reflection.PropertyInfo property in updateValues.GetType().GetProperties())
        {
            // Get column name from ColumnAttribute or use property name in lowercase
            var columnAttr = property.GetCustomAttributes(typeof(Rymote.Radiant.Core.Attributes.ColumnAttribute), true)
                .FirstOrDefault() as Rymote.Radiant.Core.Attributes.ColumnAttribute;
            string columnName = columnAttr?.Name ?? property.Name.ToLowerInvariant();
            
            // Skip primary key attributes that are auto-generated
            var pkAttr = property.GetCustomAttributes(typeof(Rymote.Radiant.Core.Attributes.PrimaryKeyAttribute), true)
                .FirstOrDefault() as Rymote.Radiant.Core.Attributes.PrimaryKeyAttribute;
            if (pkAttr != null && pkAttr.AutoGenerated)
                continue;
                
            // Also skip if property name is "Id" (common primary key)
            if (property.Name == "Id")
                continue;
                
            string paramName = $"update_{property.Name}";
            setClauses.Add($"\"{columnName}\" = @{paramName}");
            parameters.Add(paramName, property.GetValue(updateValues));
        }

        sql.Append(string.Join(", ", setClauses));

        if (whereConditions.Any())
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", whereConditions));
        }

        return await connection.ExecuteAsync(sql.ToString(), parameters);
    }

    public async Task<int> DeleteAsync()
    {
        StringBuilder sql = new StringBuilder();
        sql.Append($"DELETE FROM \"{schema}\".\"{tableName}\"");

        if (whereConditions.Any())
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", whereConditions));
        }

        return await connection.ExecuteAsync(sql.ToString(), parameters);
    }
}
