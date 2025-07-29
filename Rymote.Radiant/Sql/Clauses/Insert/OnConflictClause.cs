using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public enum OnConflictAction
{
    DoNothing,
    DoUpdate
}

public sealed class OnConflictClause : IQueryClause
{
    public string[]? ConflictColumns { get; }
    public string? ConstraintName { get; }
    public OnConflictAction Action { get; }
    public Dictionary<string, object>? UpdateValues { get; set; }
    public string? WhereCondition { get; }
    
    public OnConflictClause(string[] conflictColumns, OnConflictAction action)
    {
        ConflictColumns = conflictColumns;
        Action = action;
    }
    
    public OnConflictClause(string constraintName, OnConflictAction action)
    {
        ConstraintName = constraintName;
        Action = action;
    }
    
    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.ON_CONFLICT);
        
        if (ConflictColumns?.Length > 0)
        {
            stringBuilder.Append(SqlKeywords.SPACE).Append(SqlKeywords.OPEN_PAREN);
            for (int index = 0; index < ConflictColumns.Length; index++)
            {
                if (index > 0) 
                    stringBuilder.Append(SqlKeywords.COMMA);
                
                stringBuilder.Append(SqlKeywords.QUOTE).Append(ConflictColumns[index]).Append(SqlKeywords.QUOTE);
            }
            stringBuilder.Append(SqlKeywords.CLOSE_PAREN);
        }
        else if (!string.IsNullOrEmpty(ConstraintName))
        {
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.ON_CONSTRAINT)
                .Append(SqlKeywords.SPACE)
                .Append(ConstraintName);
        }
        
        stringBuilder.Append(SqlKeywords.SPACE).Append(Action switch
        {
            OnConflictAction.DoNothing => SqlKeywords.DO_NOTHING,
            OnConflictAction.DoUpdate => SqlKeywords.DO_UPDATE + SqlKeywords.SPACE + SqlKeywords.SET,
            _ => throw new ArgumentException("Invalid conflict action")
        });
        
        if (Action == OnConflictAction.DoUpdate && UpdateValues?.Count > 0)
        {
            stringBuilder.Append(SqlKeywords.SPACE);
            bool first = true;
            foreach ((string column, object value) in UpdateValues)
            {
                if (!first) stringBuilder.Append(SqlKeywords.COMMA);
                first = false;
                
                string paramName = parameterBag.Add(value);
                stringBuilder.Append(SqlKeywords.QUOTE).Append(column).Append(SqlKeywords.QUOTE)
                    .Append(SqlKeywords.EQUALS)
                    .Append(SqlKeywords.PARAMETER_PREFIX).Append(paramName);
            }
        }

        if (!string.IsNullOrEmpty(WhereCondition))
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.WHERE)
                .Append(SqlKeywords.SPACE)
                .Append(WhereCondition);
    }
}
