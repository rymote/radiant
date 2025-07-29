using System.Text;
using Rymote.Radiant.Sql.Clauses.Returning;
using Rymote.Radiant.Sql.Clauses.Table;
using Rymote.Radiant.Sql.Clauses.Update;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Parameters;
using Rymote.Radiant.Sql.Validation;

namespace Rymote.Radiant.Sql.Builder;

public sealed class UpdateBuilder : IQueryBuilder
{
    public UpdateClause UpdateClause { get; } = new();
    public TableClause? TableClause { get; private set; }
    public SetClause? SetClause { get; private set; }
    public WhereClause WhereClause { get; } = new();
    public ReturningClause? ReturningClause { get; private set; }

    private readonly List<(string ColumnName, object Value)> pendingAssignments = new();

    public UpdateBuilder Table(string tableName, string? schemaName = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName), 
            "Table name is required for UPDATE statement");

        TableClause ??= new TableClause(tableName, schemaName);
        return this;
    }

    public UpdateBuilder Set(string columnName, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName), 
            "Column name is required for SET clause");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "UpdateBuilder", 
            "Table", "Set");

        pendingAssignments.Add((columnName, value));

        SetClause ??= new SetClause([]);
        return this;
    }

    public UpdateBuilder Where(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName), 
            "Column name is required for WHERE condition");
        
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol), 
            "Operator symbol is required for WHERE condition (e.g., '=', '>', '<', 'LIKE')");

        QueryBuilderStateValidator.ValidateBuilderState(SetClause != null, "UpdateBuilder", 
            "Set", "Where");
        
        WhereClause.AddCondition(columnName, operatorSymbol, value);
        return this;
    }

    public UpdateBuilder Returning(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames), 
            "At least one column name is required for RETURNING clause");

        QueryBuilderStateValidator.ValidateBuilderState(SetClause != null, "UpdateBuilder", 
            "Set", "Returning");
        
        IEnumerable<string> duplicates = columnNames.GroupBy(name => name).Where(grouping => grouping.Count() > 1)
            .Select(grouping => grouping.Key).ToList();
        if (duplicates.Any())
            QueryBuilderStateValidator.ValidateBuilderState(false, "UpdateBuilder",
                $"Duplicate column names in RETURNING clause: {string.Join(", ", duplicates)}");
        
        ReturningClause = new ReturningClause(columnNames);
        return this;
    }
    
    internal void PrepareClauses(ParameterBag parameterBag)
    {
        if (SetClause is { Assignments.Count: > 0 }) return;
        
        QueryBuilderStateValidator.ValidateNotEmpty(pendingAssignments, nameof(pendingAssignments), 
            "At least one SET assignment is required before compilation");
        
        List<(string ColumnName, string ParameterName)> assignments = pendingAssignments
            .Select(valueTuple => (valueTuple.ColumnName, ParameterName: parameterBag.Add(valueTuple.Value)))
            .ToList();

        SetClause = new SetClause(assignments);
    }

    public QueryCommand Build() => QueryCompiler.Compile(this);
}