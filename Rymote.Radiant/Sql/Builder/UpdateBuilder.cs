using System.Text;
using Rymote.Radiant.Sql.Clauses.Returning;
using Rymote.Radiant.Sql.Clauses.Table;
using Rymote.Radiant.Sql.Clauses.Update;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;
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

    internal readonly List<(string ColumnName, object Value)> pendingAssignments = new();
    internal readonly List<(string ColumnName, ISqlExpression Expression)> pendingExpressionAssignments = new();

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
        return this;
    }
    
    public UpdateBuilder SetExpression(string columnName, ISqlExpression expression)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName), 
            "Column name is required for SET clause");

        QueryBuilderStateValidator.ValidateBuilderState(TableClause != null, "UpdateBuilder", 
            "Table", "SetExpression");

        pendingExpressionAssignments.Add((columnName, expression));
        return this;
    }

    public UpdateBuilder Where(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName), 
            "Column name is required for WHERE condition");
        
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol), 
            "Operator symbol is required for WHERE condition (e.g., '=', '>', '<', 'LIKE')");

        QueryBuilderStateValidator.ValidateBuilderState(pendingAssignments.Count > 0 || pendingExpressionAssignments.Count > 0, 
            "UpdateBuilder", "Set or SetExpression", "Where");
        
        WhereClause.AddCondition(columnName, operatorSymbol, value);
        return this;
    }

    public UpdateBuilder Returning(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames), 
            "At least one column name is required for RETURNING clause");

        QueryBuilderStateValidator.ValidateBuilderState(pendingAssignments.Count > 0 || pendingExpressionAssignments.Count > 0, 
            "UpdateBuilder", "Set or SetExpression", "Returning");
        
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
        if (SetClause != null && SetClause.Assignments.Count > 0) 
            return;

        List<SetAssignment> assignments = new List<SetAssignment>();
    
        foreach ((string columnName, ISqlExpression expression) in pendingExpressionAssignments)
            assignments.Add(new SetExpressionAssignment(columnName, expression));
    
        foreach ((string columnName, object value) in pendingAssignments)
        {
            string parameterName = parameterBag.Add(value);
            assignments.Add(new SetParameterAssignment(columnName, parameterName));
        }

        SetClause = new SetClause(assignments);
    }

    public QueryCommand Build() => QueryCompiler.Compile(this);
}