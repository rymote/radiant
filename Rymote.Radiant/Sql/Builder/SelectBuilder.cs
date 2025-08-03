using Rymote.Radiant.Sql.Clauses;
using Rymote.Radiant.Sql.Clauses.Cte;
using Rymote.Radiant.Sql.Clauses.From;
using Rymote.Radiant.Sql.Clauses.GroupBy;
using Rymote.Radiant.Sql.Clauses.Having;
using Rymote.Radiant.Sql.Clauses.Join;
using Rymote.Radiant.Sql.Clauses.Limit;
using Rymote.Radiant.Sql.Clauses.OrderBy;
using Rymote.Radiant.Sql.Clauses.Select;
using Rymote.Radiant.Sql.Clauses.SetOperation;
using Rymote.Radiant.Sql.Clauses.Where;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Validation;

namespace Rymote.Radiant.Sql.Builder;

public sealed class SelectBuilder : IQueryBuilder
{
    public SelectClause? SelectClause { get; private set; }
    public IQueryClause? FromClause { get; private set; }
    public WhereClause WhereClause { get; } = new();
    public GroupByClause GroupByClause { get; } = new();
    public HavingClause HavingClause { get; } = new();
    public WithClause WithClause { get; } = new();
    public IReadOnlyList<IQueryClause> AdditionalClauses => additionalClauses;
    public IReadOnlyList<SetOperationClause> SetOperations => setOperations;

    private readonly List<IQueryClause> additionalClauses = [];
    private readonly List<SetOperationClause> setOperations = [];
    private bool isDistinct;

    public SelectBuilder With(string cteName, IQueryBuilder cteQuery, params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(cteName, nameof(cteName),
            "CTE name is required");

        QueryBuilderStateValidator.ValidateNotNull(cteQuery, nameof(cteQuery),
            "CTE query cannot be null");

        CteClause cte = new CteClause(cteName, cteQuery, columnNames, isRecursive: false);
        WithClause.AddCte(cte);
        return this;
    }

    public SelectBuilder WithRecursive(string cteName, IQueryBuilder cteQuery, params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(cteName, nameof(cteName),
            "Recursive CTE name is required");

        QueryBuilderStateValidator.ValidateNotNull(cteQuery, nameof(cteQuery),
            "Recursive CTE query cannot be null");

        CteClause cte = new CteClause(cteName, cteQuery, columnNames, isRecursive: true);
        WithClause.AddCte(cte);
        return this;
    }

    public SelectBuilder Select(params ISqlExpression[] columnExpressions)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnExpressions, nameof(columnExpressions),
            "At least one column expression is required for SELECT clause");

        SelectClause = new SelectClause(columnExpressions);
        return this;
    }

    public SelectBuilder SelectDistinct(params ISqlExpression[] columnExpressions)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnExpressions, nameof(columnExpressions),
            "At least one column expression is required for SELECT DISTINCT clause");

        isDistinct = true;
        SelectClause = new SelectClause(columnExpressions);
        return this;
    }

    public WindowFunctionExpression RowNumber()
    {
        return new WindowFunctionExpression("ROW_NUMBER");
    }

    public WindowFunctionExpression Rank()
    {
        return new WindowFunctionExpression("RANK");
    }

    public WindowFunctionExpression DenseRank()
    {
        return new WindowFunctionExpression("DENSE_RANK");
    }

    public WindowFunctionExpression Lead(ISqlExpression column, int offset = 1, object? defaultValue = null)
    {
        List<ISqlExpression> expressions = [column];
        if (offset != 1) expressions.Add(new RawSqlExpression(offset.ToString()));
        if (defaultValue != null) expressions.Add(new RawSqlExpression($"'{defaultValue}'"));

        return new WindowFunctionExpression("LEAD", expressions.ToArray());
    }

    public WindowFunctionExpression Lag(ISqlExpression column, int offset = 1, object? defaultValue = null)
    {
        List<ISqlExpression> expressions = [column];
        if (offset != 1) expressions.Add(new RawSqlExpression(offset.ToString()));
        if (defaultValue != null) expressions.Add(new RawSqlExpression($"'{defaultValue}'"));

        return new WindowFunctionExpression("LAG", expressions.ToArray());
    }

    public WindowFunctionExpression FirstValue(ISqlExpression column)
    {
        return new WindowFunctionExpression("FIRST_VALUE", column);
    }

    public WindowFunctionExpression LastValue(ISqlExpression column)
    {
        return new WindowFunctionExpression("LAST_VALUE", column);
    }

    public WindowFunctionExpression NthValue(ISqlExpression column, int n)
    {
        return new WindowFunctionExpression("NTH_VALUE", column, new RawSqlExpression(n.ToString()));
    }

    public WindowFunctionExpression Sum(ISqlExpression column)
    {
        return new WindowFunctionExpression("SUM", column);
    }

    public WindowFunctionExpression Avg(ISqlExpression column)
    {
        return new WindowFunctionExpression("AVG", column);
    }

    public WindowFunctionExpression Count(ISqlExpression? column = null)
    {
        return new WindowFunctionExpression("COUNT", column ?? new RawSqlExpression("*"));
    }

    public WindowFunctionExpression Min(ISqlExpression column)
    {
        return new WindowFunctionExpression("MIN", column);
    }

    public WindowFunctionExpression Max(ISqlExpression column)
    {
        return new WindowFunctionExpression("MAX", column);
    }

    public SelectBuilder From(string tableName, string? schemaName = null, string? alias = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName),
            "Table name is required for FROM clause");

        QueryBuilderStateValidator.ValidateBuilderState(SelectClause != null, "SelectBuilder",
            "Select", "From");

        FromClause = new Clauses.From.FromClause(tableName, schemaName, alias);
        return this;
    }

    public SelectBuilder From(IQueryBuilder subquery, string alias)
    {
        QueryBuilderStateValidator.ValidateNotNull(subquery, nameof(subquery),
            "Subquery cannot be null");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(alias, nameof(alias),
            "Alias is required for subquery in FROM clause");

        QueryBuilderStateValidator.ValidateBuilderState(SelectClause != null, "SelectBuilder",
            "Select", "From");

        FromClause = new SubqueryFromClause(subquery, alias);
        return this;
    }

    public SelectBuilder Where(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for WHERE condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for WHERE condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "Where");

        WhereClause.Where(columnName, operatorSymbol, value);
        return this;
    }

    public SelectBuilder Where(string columnName, string operatorSymbol, IQueryBuilder subquery)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for WHERE condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for WHERE condition");

        QueryBuilderStateValidator.ValidateNotNull(subquery, nameof(subquery),
            "Subquery cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "Where");

        SubqueryExpression subqueryExpression = new SubqueryExpression(subquery);
        WhereClause.Where(columnName, operatorSymbol, subqueryExpression);
        return this;
    }

    public SelectBuilder WhereNull(string columnName)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for WHERE IS NULL condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "WhereNull");

        WhereClause.Where(columnName, "IS", (object)null);
        return this;
    }

    public SelectBuilder WhereNotNull(string columnName)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for WHERE IS NOT NULL condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "WhereNotNull");

        WhereClause.Where(columnName, "IS NOT", (object)null);
        return this;
    }
    
    public SelectBuilder WhereExists(IQueryBuilder subquery)
    {
        QueryBuilderStateValidator.ValidateNotNull(subquery, nameof(subquery),
            "Subquery cannot be null for EXISTS condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "WhereExists");

        SubqueryExpression subqueryExpression = new SubqueryExpression(subquery);
        WhereClause.Where(SqlKeywords.EXISTS, "", subqueryExpression);
        return this;
    }

    public SelectBuilder WhereNotExists(IQueryBuilder subquery)
    {
        QueryBuilderStateValidator.ValidateNotNull(subquery, nameof(subquery),
            "Subquery cannot be null for NOT EXISTS condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "WhereNotExists");

        SubqueryExpression subqueryExpression = new SubqueryExpression(subquery);
        WhereClause.Where(SqlKeywords.NOT_EXISTS, "", subqueryExpression);
        return this;
    }
    
    public SelectBuilder WhereBooleanExpression(ISqlExpression booleanExpression)
    {
        QueryBuilderStateValidator.ValidateNotNull(booleanExpression, nameof(booleanExpression),
            "Boolean expression cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "WhereBooleanExpression");

        WhereClause.AddBooleanExpression(booleanExpression);
        return this;
    }

    public SelectBuilder And(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for AND condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for AND condition");

        QueryBuilderStateValidator.ValidateBuilderState(WhereClause.HasConditions, "SelectBuilder",
            "Where", "And");

        WhereClause.And(columnName, operatorSymbol, value);
        return this;
    }

    public SelectBuilder Or(string columnName, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for OR condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for OR condition");

        QueryBuilderStateValidator.ValidateBuilderState(WhereClause.HasConditions, "SelectBuilder",
            "Where", "Or");

        WhereClause.Or(columnName, operatorSymbol, value);
        return this;
    }

    public SelectBuilder And(Action<WhereGroup> whereBuilder)
    {
        QueryBuilderStateValidator.ValidateNotNull(whereBuilder, nameof(whereBuilder),
            "WHERE group builder cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(WhereClause.HasConditions, "SelectBuilder",
            "Where", "And");

        WhereClause.And(whereBuilder);
        return this;
    }

    public SelectBuilder Or(Action<WhereGroup> whereBuilder)
    {
        QueryBuilderStateValidator.ValidateNotNull(whereBuilder, nameof(whereBuilder),
            "WHERE group builder cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(WhereClause.HasConditions, "SelectBuilder",
            "Where", "Or");

        WhereClause.Or(whereBuilder);
        return this;
    }

    public SelectBuilder Having(string expression, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(expression, nameof(expression),
            "Expression is required for HAVING condition (e.g., 'COUNT(id)', 'SUM(amount)')");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for HAVING condition");

        QueryBuilderStateValidator.ValidateBuilderState(GroupByClause.HasColumns, "SelectBuilder",
            "GroupBy", "Having");

        HavingClause.Having(expression, operatorSymbol, value);
        return this;
    }

    public SelectBuilder HavingAnd(string expression, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(expression, nameof(expression),
            "Expression is required for HAVING AND condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for HAVING AND condition");

        QueryBuilderStateValidator.ValidateBuilderState(HavingClause.HasConditions, "SelectBuilder",
            "Having", "HavingAnd");

        HavingClause.And(expression, operatorSymbol, value);
        return this;
    }

    public SelectBuilder HavingOr(string expression, string operatorSymbol, object value)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(expression, nameof(expression),
            "Expression is required for HAVING OR condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(operatorSymbol, nameof(operatorSymbol),
            "Operator symbol is required for HAVING OR condition");

        QueryBuilderStateValidator.ValidateBuilderState(HavingClause.HasConditions, "SelectBuilder",
            "Having", "HavingOr");

        HavingClause.Or(expression, operatorSymbol, value);
        return this;
    }

    public SelectBuilder HavingAnd(Action<WhereGroup> havingBuilder)
    {
        QueryBuilderStateValidator.ValidateNotNull(havingBuilder, nameof(havingBuilder),
            "HAVING group builder cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(HavingClause.HasConditions, "SelectBuilder",
            "Having", "HavingAnd");

        HavingClause.And(havingBuilder);
        return this;
    }

    public SelectBuilder HavingOr(Action<WhereGroup> havingBuilder)
    {
        QueryBuilderStateValidator.ValidateNotNull(havingBuilder, nameof(havingBuilder),
            "HAVING group builder cannot be null");

        QueryBuilderStateValidator.ValidateBuilderState(HavingClause.HasConditions, "SelectBuilder",
            "Having", "HavingOr");

        HavingClause.Or(havingBuilder);
        return this;
    }

    public SelectBuilder GroupBy(params string[] columnNames)
    {
        QueryBuilderStateValidator.ValidateNotEmpty(columnNames, nameof(columnNames),
            "At least one column name is required for GROUP BY clause");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "GroupBy");

        GroupByClause.AddColumns(columnNames);
        return this;
    }

    public SelectBuilder Join(
        JoinType joinType, 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName),
            "Table name is required for JOIN clause");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(alias, nameof(alias),
            "Table alias is required for JOIN clause");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(leftColumn, nameof(leftColumn),
            "Left column name is required for JOIN condition");

        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(rightColumn, nameof(rightColumn),
            "Right column name is required for JOIN condition");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "Join");

        additionalClauses.Add(new JoinClause(joinType, tableName, schemaName, alias, leftColumn, rightColumn));
        return this;
    }

    public SelectBuilder InnerJoin( 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        return Join(JoinType.Inner, tableName, leftColumn, rightColumn, schemaName, alias);
    }

    public SelectBuilder LeftJoin( 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        return Join(JoinType.Left, tableName, leftColumn, rightColumn, schemaName, alias);
    }

    public SelectBuilder RightJoin( 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        return Join(JoinType.Right, tableName, leftColumn, rightColumn, schemaName, alias);
    }

    public SelectBuilder FullJoin( 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        return Join(JoinType.Full, tableName, leftColumn, rightColumn, schemaName, alias);
    }

    public SelectBuilder FullOuterJoin( 
        string tableName,
        string leftColumn, 
        string rightColumn, 
        string? schemaName = null, 
        string? alias = null)
    {
        return Join(JoinType.FullOuter, tableName, leftColumn, rightColumn, schemaName, alias);
    }

    public SelectBuilder CrossJoin(string tableName, string? schemaName = null, string? alias = null)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(tableName, nameof(tableName),
            "Table name is required for CROSS JOIN clause");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "CrossJoin");

        additionalClauses.Add(new CrossJoinClause(tableName, schemaName, alias));
        return this;
    }

    public SelectBuilder LateralJoin(IQueryBuilder subQuery, string alias)
    {
        QueryBuilderStateValidator.ValidateNotNull(subQuery, nameof(subQuery), 
            "Subquery cannot be null for LATERAL JOIN");
    
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(alias, nameof(alias), 
            "Alias is required for LATERAL JOIN");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder", 
            "From", "LateralJoin");

        additionalClauses.Add(new LateralJoinClause(JoinType.Inner, subQuery, alias));
        return this;
    }

    public SelectBuilder LeftLateralJoin(IQueryBuilder subQuery, string alias)
    {
        QueryBuilderStateValidator.ValidateNotNull(subQuery, nameof(subQuery), 
            "Subquery cannot be null for LEFT LATERAL JOIN");
    
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(alias, nameof(alias), 
            "Alias is required for LEFT LATERAL JOIN");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder", 
            "From", "LeftLateralJoin");

        additionalClauses.Add(new LateralJoinClause(JoinType.Left, subQuery, alias));
        return this;
    }

    public SelectBuilder CrossLateralJoin(IQueryBuilder subQuery, string alias)
    {
        QueryBuilderStateValidator.ValidateNotNull(subQuery, nameof(subQuery), 
            "Subquery cannot be null for CROSS LATERAL JOIN");
    
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(alias, nameof(alias), 
            "Alias is required for CROSS LATERAL JOIN");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder", 
            "From", "CrossLateralJoin");

        additionalClauses.Add(new LateralJoinClause(JoinType.Cross, subQuery, alias));
        return this;
    }
    
    public SelectBuilder OrderBy(string columnName, SortDirection sortDirection = SortDirection.Ascending)
    {
        QueryBuilderStateValidator.ValidateNotNullOrWhiteSpace(columnName, nameof(columnName),
            "Column name is required for ORDER BY clause");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "OrderBy");

        additionalClauses.Add(new OrderByClause(columnName, sortDirection));
        return this;
    }

    public SelectBuilder Limit(int limit, int? offset = null)
    {
        QueryBuilderStateValidator.ValidatePositive(limit, nameof(limit),
            "LIMIT value must be greater than zero");

        if (offset.HasValue)
            QueryBuilderStateValidator.ValidateNonNegative(offset.Value, nameof(offset),
                "OFFSET value must be greater than or equal to zero");

        QueryBuilderStateValidator.ValidateBuilderState(FromClause != null, "SelectBuilder",
            "From", "Limit");

        additionalClauses.Add(new LimitClause(limit, offset));
        return this;
    }

    public SelectBuilder Union(IQueryBuilder otherQuery)
    {
        QueryBuilderStateValidator.ValidateNotNull(otherQuery, nameof(otherQuery),
            "Query cannot be null for UNION operation");

        setOperations.Add(new SetOperationClause(SetOperationType.Union, otherQuery));
        return this;
    }

    public SelectBuilder UnionAll(IQueryBuilder otherQuery)
    {
        QueryBuilderStateValidator.ValidateNotNull(otherQuery, nameof(otherQuery),
            "Query cannot be null for UNION ALL operation");

        setOperations.Add(new SetOperationClause(SetOperationType.UnionAll, otherQuery));
        return this;
    }

    public SelectBuilder Intersect(IQueryBuilder otherQuery)
    {
        QueryBuilderStateValidator.ValidateNotNull(otherQuery, nameof(otherQuery),
            "Query cannot be null for INTERSECT operation");

        setOperations.Add(new SetOperationClause(SetOperationType.Intersect, otherQuery));
        return this;
    }

    public SelectBuilder Except(IQueryBuilder otherQuery)
    {
        QueryBuilderStateValidator.ValidateNotNull(otherQuery, nameof(otherQuery),
            "Query cannot be null for EXCEPT operation");

        setOperations.Add(new SetOperationClause(SetOperationType.Except, otherQuery));
        return this;
    }

    public bool IsDistinct => isDistinct;

    public QueryCommand Build() => QueryCompiler.Compile(this);
}