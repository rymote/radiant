using Rymote.Radiant.Sql.Clauses.Delete;
using Rymote.Radiant.Sql.Clauses.From;
using Rymote.Radiant.Sql.Clauses.Returning;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Validation;

namespace Rymote.Radiant.Sql.Builder;

public sealed class DeleteBuilder : IQueryBuilder
{
    public DeleteClause DeleteClause { get; } = new();
    public FromClause? FromClause { get; private set; }
    public WhereClause WhereClause { get; } = new();
    public ReturningClause? ReturningClause { get; private set; }

    public DeleteBuilder From(string tableName, string? schemaName = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName), 
            "Table name is required for DELETE FROM clause");

        FromClause ??= new FromClause(tableName, schemaName);
        return this;
    }

    public DeleteBuilder Where(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName), 
            "Column name is required for WHERE condition");
        
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol), 
            "Operator symbol is required for WHERE condition (e.g., '=', '>', '<', 'LIKE')");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "DeleteBuilder", 
            "From", "Where");
        
        WhereClause.AddCondition(columnName, operatorSymbol, value);
        return this;
    }
    
    public DeleteBuilder Returning(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames), 
            "At least one column name is required for RETURNING clause");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "DeleteBuilder", 
            "From", "Returning");
        
        IEnumerable<string> duplicates = columnNames.GroupBy(name => name).Where(grouping => grouping.Count() > 1)
            .Select(grouping => grouping.Key).ToList();
        if (duplicates.Any())
            QueryBuilderStateValidator.ValidateBuilderState(false, "DeleteBuilder",
                $"Duplicate column names in RETURNING clause: {string.Join(", ", duplicates)}");
        
        ReturningClause = new ReturningClause(columnNames);
        return this;
    }

    public QueryCommand Build() => QueryCompiler.Compile(this);
}