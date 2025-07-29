using System.Text;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Clauses;
using Rymote.Radiant.Sql.Clauses.Join;
using Rymote.Radiant.Sql.Clauses.SetOperation;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Exceptions;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;
using Rymote.Radiant.Sql.Validation;

namespace Rymote.Radiant.Sql.Compiler;

public static class QueryCompiler
{
    public static QueryCommand Compile(IQueryBuilder builder) =>
        builder switch
        {
            SelectBuilder selectBuilder => Compile(selectBuilder),
            InsertBuilder insertBuilder => Compile(insertBuilder),
            UpdateBuilder updateBuilder => Compile(updateBuilder),
            DeleteBuilder deleteBuilder => Compile(deleteBuilder),
            _ => throw new UnsupportedQueryOperationException($"Query compilation for builder type '{builder.GetType().Name}'", 
                "QueryCompiler.Compile")
        };
    
    public static QueryCommand Compile(SelectBuilder selectBuilder)
    {
        QueryBuilderStateValidator.ValidateForCompilation(selectBuilder.SelectClause != null, 
            "SelectBuilder", "SelectClause");
        
        QueryBuilderStateValidator.ValidateForCompilation(selectBuilder.FromClause != null, 
            "SelectBuilder", "FromClause");

        try
        {
            ParameterBag parameterBag = new ParameterBag();
            StringBuilder stringBuilder = new StringBuilder();

            selectBuilder.WithClause.AppendTo(stringBuilder, parameterBag);

            if (selectBuilder.IsDistinct)
            {
                stringBuilder
                    .Append(SqlKeywords.SELECT)
                    .Append(SqlKeywords.SPACE)
                    .Append(SqlKeywords.DISTINCT)
                    .Append(SqlKeywords.SPACE);
                
                IReadOnlyList<ISqlExpression> expressions = selectBuilder.SelectClause!.Expressions;
                for (int index = 0; index < expressions.Count; index++)
                {
                    if (index > 0) 
                        stringBuilder.Append(SqlKeywords.COMMA);
                    
                    expressions[index].AppendTo(stringBuilder);
                }
            }
            else
            {
                selectBuilder.SelectClause!.AppendTo(stringBuilder, parameterBag);
            }

            selectBuilder.FromClause!.AppendTo(stringBuilder, parameterBag);

            foreach (IQueryClause queryClause in selectBuilder.AdditionalClauses)
                if (queryClause is JoinClause or CrossJoinClause)
                    queryClause.AppendTo(stringBuilder, parameterBag);

            selectBuilder.WhereClause.AppendTo(stringBuilder, parameterBag);
            selectBuilder.GroupByClause.AppendTo(stringBuilder, parameterBag);
            selectBuilder.HavingClause.AppendTo(stringBuilder, parameterBag);

            foreach (SetOperationClause setOperation in selectBuilder.SetOperations)
                setOperation.AppendTo(stringBuilder, parameterBag);

            foreach (IQueryClause queryClause in selectBuilder.AdditionalClauses)
                if (queryClause is not (JoinClause or CrossJoinClause))
                    queryClause.AppendTo(stringBuilder, parameterBag);

            return new QueryCommand(stringBuilder.ToString(), parameterBag.ToDynamicParameters());
        }
        catch (Exception exception) when (exception is not QueryBuilderException)
        {
            throw new QueryCompilationException("SelectBuilder", 
                $"Unexpected error during compilation: {exception.Message}");
        }
    }

    public static QueryCommand Compile(InsertBuilder insertBuilder)
    {
        QueryBuilderStateValidator.ValidateForCompilation(insertBuilder.TableClause != null, 
            "InsertBuilder", "TableClause (call Into())");

        bool hasSingleValues = insertBuilder.ValuesClause != null || insertBuilder.columnValues?.Count > 0;
        bool hasMultipleValues = insertBuilder.MultipleValuesClause?.RowCount > 0;
        bool hasSelect = insertBuilder.SelectQuery != null;
        
        QueryBuilderStateValidator.ValidateForCompilation(hasSingleValues || hasMultipleValues || hasSelect, 
            "InsertBuilder", "ValuesClause (call Value()), MultipleValuesClause (call Values()), or SelectQuery (call Select())");

        try
        {
            ParameterBag parameterBag = new ParameterBag();
            StringBuilder stringBuilder = new StringBuilder();

            insertBuilder.InsertClause.AppendTo(stringBuilder, parameterBag);
            insertBuilder.TableClause!.AppendTo(stringBuilder, parameterBag);

            if (insertBuilder.SelectQuery != null)
            {
                if (insertBuilder.targetColumns.Count > 0)
                {
                    stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);
                    for (int index = 0; index < insertBuilder.targetColumns.Count; index++)
                    {
                        if (index > 0) 
                            stringBuilder.Append(SqlKeywords.COMMA);
                        
                        stringBuilder.Append(SqlKeywords.QUOTE).Append(insertBuilder.targetColumns[index]).Append(SqlKeywords.QUOTE);
                    }
                    
                    stringBuilder.Append(SqlKeywords.CLOSE_PAREN).Append(SqlKeywords.SPACE);
                }
                else
                {
                    stringBuilder.Append(SqlKeywords.SPACE);
                }

                QueryCommand selectCommand = insertBuilder.SelectQuery.Build();
                stringBuilder.Append(selectCommand.SqlText);

                foreach (string parameterName in selectCommand.Parameters.ParameterNames)
                {
                    object? value = selectCommand.Parameters.Get<object>(parameterName);
                    parameterBag.Add(value);
                }
            }
            else if (insertBuilder.MultipleValuesClause != null)
            {
                insertBuilder.MultipleValuesClause.AppendTo(stringBuilder, parameterBag);
            }
            else
            {
                insertBuilder.PrepareClauses(parameterBag);
                insertBuilder.ValuesClause!.AppendTo(stringBuilder, parameterBag);
            }

            insertBuilder.ReturningClause?.AppendTo(stringBuilder, parameterBag);

            return new QueryCommand(stringBuilder.ToString(), parameterBag.ToDynamicParameters());
        }
        catch (Exception exception) when (exception is not QueryBuilderException)
        {
            throw new QueryCompilationException("InsertBuilder", 
                $"Unexpected error during compilation: {exception.Message}");
        }
    }

    public static QueryCommand Compile(UpdateBuilder updateBuilder)
    {
        QueryBuilderStateValidator.ValidateForCompilation(updateBuilder.TableClause != null, 
            "UpdateBuilder", "TableClause");
        
        QueryBuilderStateValidator.ValidateForCompilation(updateBuilder.SetClause != null, 
            "UpdateBuilder", "SetClause");
        
        try
        {
            ParameterBag parameterBag = new ParameterBag();
            StringBuilder stringBuilder = new StringBuilder();

            updateBuilder.PrepareClauses(parameterBag);

            updateBuilder.UpdateClause.AppendTo(stringBuilder, parameterBag);
            updateBuilder.TableClause!.AppendTo(stringBuilder, parameterBag);
            updateBuilder.SetClause!.AppendTo(stringBuilder, parameterBag);
            updateBuilder.WhereClause.AppendTo(stringBuilder, parameterBag);
            updateBuilder.ReturningClause?.AppendTo(stringBuilder, parameterBag);

            return new QueryCommand(stringBuilder.ToString(), parameterBag.ToDynamicParameters());
        }
        catch (Exception exception) when (exception is not QueryBuilderException)
        {
            throw new QueryCompilationException("UpdateBuilder", 
                $"Unexpected error during compilation: {exception.Message}");
        }
    }
    
    public static QueryCommand Compile(DeleteBuilder deleteBuilder)
    {
        QueryBuilderStateValidator.ValidateForCompilation(deleteBuilder.FromClause != null, 
            "DeleteBuilder", "FromClause");
        
        try 
        {
            ParameterBag parameterBag = new ParameterBag();
            StringBuilder stringBuilder = new StringBuilder();
            
            deleteBuilder.DeleteClause.AppendTo(stringBuilder, parameterBag);
            deleteBuilder.FromClause!.AppendTo(stringBuilder, parameterBag);
            deleteBuilder.WhereClause.AppendTo(stringBuilder, parameterBag);
            deleteBuilder.ReturningClause?.AppendTo(stringBuilder, parameterBag);
            
            return new QueryCommand(stringBuilder.ToString(), parameterBag.ToDynamicParameters());
        }
        catch (Exception exception) when (exception is not QueryBuilderException)
        {
            throw new QueryCompilationException("DeleteBuilder", 
                $"Unexpected error during compilation: {exception.Message}");
        }
    }
}
