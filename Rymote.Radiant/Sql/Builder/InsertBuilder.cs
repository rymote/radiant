using System.Reflection;
using System.Text;
using Rymote.Radiant.Sql.Clauses.Insert;
using Rymote.Radiant.Sql.Clauses.Returning;
using Rymote.Radiant.Sql.Clauses.Table;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;
using Rymote.Radiant.Sql.Validation;

namespace Rymote.Radiant.Sql.Builder;

public sealed class InsertBuilder : IQueryBuilder
{
    public InsertClause InsertClause { get; } = new();
    public TableClause? TableClause { get; private set; }
    public ValuesClause? ValuesClause { get; private set; }
    public ValuesExpressionClause? ValuesExpressionClause { get; private set; }
    public MultipleValuesClause? MultipleValuesClause { get; private set; }
    public IQueryBuilder? SelectQuery { get; private set; }
    public OnConflictClause? OnConflictClause { get; private set; }
    public ReturningClause? ReturningClause { get; private set; }

    internal readonly List<(string ColumnName, object Value)> columnValues = [];
    internal readonly List<(string ColumnName, ISqlExpression Expression)> columnExpressions = [];
    internal readonly List<string> targetColumns = [];
    private bool isMultipleValues = false;

    public InsertBuilder Into(string tableName, string? schemaName = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName),
            "Table name is required for INSERT INTO clause");

        TableClause = new TableClause(tableName, schemaName);
        return this;
    }

    public InsertBuilder Value(string columnName, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for VALUES clause");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "InsertBuilder",
            "Into", "Value");

        if (columnValues.Any(columnValue => columnValue.ColumnName == columnName))
            QueryBuilderStateValidator.ValidateBuilderState(false, "InsertBuilder",
                $"Column '{columnName}' has already been specified in the VALUES clause. Each column can only be specified once.");

        columnValues.Add((columnName, value));

        ValuesClause ??= new ValuesClause([], []);
        return this;
    }

    public InsertBuilder ValueExpression(string columnName, ISqlExpression expression)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for VALUES clause");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "InsertBuilder",
            "Into", "ValueExpression");

        if (columnExpressions.Any(columnExpr => columnExpr.ColumnName == columnName))
            QueryBuilderStateValidator.ValidateBuilderState(false, "InsertBuilder",
                $"Column '{columnName}' has already been specified in the VALUES clause. Each column can only be specified once.");

        columnExpressions.Add((columnName, expression));

        ValuesExpressionClause ??= new ValuesExpressionClause([], []);
        return this;
    }
    
    public InsertBuilder Columns(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames), 
            "At least one column name is required");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "InsertBuilder", 
            "Into", "Columns");

        QueryBuilderStateValidator.ValidateBuilderState(columnValues.Count == 0, "InsertBuilder", 
            "Cannot specify columns when using single Value() approach. Use either Columns() + Values() OR Value().");

        QueryBuilderStateValidator.ValidateBuilderState(SelectQuery == null, "InsertBuilder", 
            "Cannot specify columns when using Select() approach. Use either Columns() + Values() OR Select().");

        targetColumns.Clear();
        targetColumns.AddRange(columnNames);
        
        if (MultipleValuesClause == null)
        {
            MultipleValuesClause = new MultipleValuesClause(targetColumns);
            isMultipleValues = true;
        }

        return this;
    }

    public InsertBuilder Values(params object[] values)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(values, nameof(values), 
            "At least one value is required for VALUES row");

        QueryBuilderStateValidator.ValidateBuilderState(MultipleValuesClause != null, "InsertBuilder", 
            "Columns", "Values");

        QueryBuilderStateValidator.ValidateBuilderState(values.Length == targetColumns.Count, "InsertBuilder", 
            $"Value count ({values.Length}) must match column count ({targetColumns.Count}).");

        MultipleValuesClause!.AddValueRow(values);
        return this;
    }

    public InsertBuilder Values<T>(T entity) where T : class
    {
        QueryBuilderStateValidator.ValidateNotNull(entity, nameof(entity), 
            "Entity cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(MultipleValuesClause != null, "InsertBuilder", 
            "Columns", "Values");

        Type entityType = typeof(T);
        List<object> values = [];

        foreach (string columnName in targetColumns)
        {
            PropertyInfo? property = entityType.GetProperty(columnName, 
                BindingFlags.IgnoreCase | 
                BindingFlags.Public | 
                BindingFlags.Instance);
            
            if (property == null)
                throw new ArgumentException($"Property '{columnName}' not found on type '{entityType.Name}'");

            object? value = property.GetValue(entity);
            values.Add(value ?? null);
        }

        MultipleValuesClause!.AddValueRow(values);
        return this;
    }

    public InsertBuilder Select(IQueryBuilder selectQuery)
    {
        QueryBuilderStateValidator.ValidateNotNull(selectQuery, nameof(selectQuery), 
            "SELECT query cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "InsertBuilder", 
            "Into", "Select");

        QueryBuilderStateValidator.ValidateBuilderState(columnValues.Count == 0, "InsertBuilder", 
            "Cannot use Select() when Value() has been called. Use either Value() OR Select(), not both.");

        QueryBuilderStateValidator.ValidateBuilderState(!isMultipleValues, "InsertBuilder", 
            "Cannot use Select() when Values() has been called. Use either Values() OR Select(), not both.");
        
        SelectQuery = selectQuery;
        return this;
    }
    
    public InsertBuilder OnConflict(params string[] columns)
    {
        OnConflictClause = new OnConflictClause(columns, OnConflictAction.DoNothing);
        return this;
    }

    public InsertBuilder OnConflictDoUpdate(string[] columns, Dictionary<string, object> updateValues)
    {
        OnConflictClause = new OnConflictClause(columns, OnConflictAction.DoUpdate)
        {
            UpdateValues = updateValues
        };
        
        return this;
    }
    
    public InsertBuilder Returning(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames),
            "At least one column name is required for RETURNING clause");

        QueryBuilderStateValidator.ValidateBuilderState(columnValues.Count > 0, "InsertBuilder",
            "Value", "Returning");

        IEnumerable<string> duplicates = columnNames.GroupBy(name => name).Where(grouping => grouping.Count() > 1)
            .Select(grouping => grouping.Key).ToList();
        if (duplicates.Any())
            QueryBuilderStateValidator.ValidateBuilderState(false, "InsertBuilder",
                $"Duplicate column names in RETURNING clause: {string.Join(", ", duplicates)}");

        ReturningClause = new ReturningClause(columnNames);
        return this;
    }

    internal void PrepareClauses(ParameterBag parameterBag)
    {
        if (columnExpressions.Count > 0 && columnValues.Count > 0)
        {
            foreach ((string columnName, object value) in columnValues)
            {
                string parameterName = parameterBag.Add(value);
                ISqlExpression parameterExpression = new RawSqlExpression($"@{parameterName}");
                columnExpressions.Add((columnName, parameterExpression));
            }
        
            columnValues.Clear();
        }
    
        if (columnExpressions.Count > 0)
        {
            List<string> columnNames = columnExpressions.Select(expression => expression.ColumnName).ToList();
            List<ISqlExpression> expressions = columnExpressions.Select(expression => expression.Expression).ToList();
        
            ValuesExpressionClause = new ValuesExpressionClause(columnNames, expressions);
        }
        else if (columnValues.Count > 0)
        {
            List<string> columnNames = columnValues.Select(valueTuple => valueTuple.ColumnName).ToList();
            List<string> parameterNames = columnValues
                .Select(valueTuple => parameterBag.Add(valueTuple.Value))
                .ToList();

            ValuesClause = new ValuesClause(columnNames, parameterNames);
        }
    }

    public QueryCommand Build() => QueryCompiler.Compile(this);
}