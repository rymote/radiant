using System.Text;
using Rymote.Radiant.Sql.Compiler;
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

    /// <summary>
    /// Column names whose update value should be sourced from the dialect's EXCLUDED pseudo-table
    /// (e.g. <c>"email" = EXCLUDED."email"</c> on PostgreSQL/SQLite). When set, takes precedence
    /// over <see cref="UpdateValues"/> for the listed columns; columns can also appear in both
    /// (excluded reference wins for that column).
    /// </summary>
    public List<string>? UpdateFromExcludedColumns { get; set; }

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
        
        if (Action == OnConflictAction.DoUpdate)
        {
            bool firstAssignment = true;

            if (UpdateFromExcludedColumns is { Count: > 0 })
            {
                stringBuilder.Append(SqlKeywords.SPACE);
                foreach (string columnName in UpdateFromExcludedColumns)
                {
                    if (!firstAssignment) stringBuilder.Append(SqlKeywords.COMMA);
                    firstAssignment = false;

                    stringBuilder
                        .Append(SqlKeywords.QUOTE).Append(columnName).Append(SqlKeywords.QUOTE)
                        .Append(SqlKeywords.EQUALS)
                        .Append(SqlKeywords.EXCLUDED).Append(SqlKeywords.DOT)
                        .Append(SqlKeywords.QUOTE).Append(columnName).Append(SqlKeywords.QUOTE);
                }
            }

            if (UpdateValues?.Count > 0)
            {
                if (firstAssignment) stringBuilder.Append(SqlKeywords.SPACE);
                foreach ((string column, object value) in UpdateValues)
                {
                    if (UpdateFromExcludedColumns is not null && UpdateFromExcludedColumns.Contains(column))
                        continue;

                    if (!firstAssignment) stringBuilder.Append(SqlKeywords.COMMA);
                    firstAssignment = false;

                    string paramName = parameterBag.Add(value);
                    stringBuilder.Append(SqlKeywords.QUOTE).Append(column).Append(SqlKeywords.QUOTE)
                        .Append(SqlKeywords.EQUALS)
                        .Append(SqlKeywords.PARAMETER_PREFIX).Append(paramName);
                }
            }
        }

        if (!string.IsNullOrEmpty(WhereCondition))
            stringBuilder
                .Append(SqlKeywords.SPACE)
                .Append(SqlKeywords.WHERE)
                .Append(SqlKeywords.SPACE)
                .Append(WhereCondition);
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteKeyword(emitter.Dialect.OnConflict);

        if (ConflictColumns?.Length > 0)
        {
            emitter.WriteSpace().WriteRaw("(");
            for (int index = 0; index < ConflictColumns.Length; index++)
            {
                if (index > 0)
                    emitter.WriteRaw(", ");

                emitter.WriteIdentifier(ConflictColumns[index]);
            }
            emitter.WriteRaw(")");
        }
        else if (!string.IsNullOrEmpty(ConstraintName))
        {
            emitter.WriteSpace().WriteRaw("ON CONSTRAINT").WriteSpace().WriteRaw(ConstraintName);
        }

        emitter.WriteSpace();

        switch (Action)
        {
            case OnConflictAction.DoNothing:
                emitter.WriteKeyword(emitter.Dialect.DoNothing);
                break;

            case OnConflictAction.DoUpdate:
                emitter.WriteKeyword(emitter.Dialect.DoUpdate).WriteSpace().WriteKeyword(emitter.Dialect.Set);
                break;

            default:
                throw new ArgumentException("Invalid conflict action");
        }

        if (Action == OnConflictAction.DoUpdate)
        {
            bool firstAssignment = true;

            if (UpdateFromExcludedColumns is { Count: > 0 })
            {
                emitter.WriteSpace();
                foreach (string columnName in UpdateFromExcludedColumns)
                {
                    if (!firstAssignment) emitter.WriteRaw(", ");
                    firstAssignment = false;

                    emitter.WriteIdentifier(columnName).WriteRaw(" = ")
                        .WriteKeyword(emitter.Dialect.ExcludedTableAlias)
                        .WriteRaw(".")
                        .WriteIdentifier(columnName);
                }
            }

            if (UpdateValues?.Count > 0)
            {
                if (firstAssignment) emitter.WriteSpace();
                foreach ((string column, object value) in UpdateValues)
                {
                    if (UpdateFromExcludedColumns is not null && UpdateFromExcludedColumns.Contains(column))
                        continue;

                    if (!firstAssignment) emitter.WriteRaw(", ");
                    firstAssignment = false;

                    emitter.WriteIdentifier(column).WriteRaw(" = ").WritePlaceholderForValue(value);
                }
            }
        }

        if (!string.IsNullOrEmpty(WhereCondition))
            emitter.WriteSpace()
                .WriteKeyword(emitter.Dialect.Where)
                .WriteSpace()
                .WriteRaw(WhereCondition);
    }
}
