using Rymote.Radiant.Sql.Compiler;

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
