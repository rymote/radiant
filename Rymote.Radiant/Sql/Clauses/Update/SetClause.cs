using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class SetClause : IQueryClause
{
    public IReadOnlyList<SetAssignment> Assignments { get; }

    public SetClause(IReadOnlyList<(string ColumnName, string ParameterName)> assignments)
    {
        List<SetAssignment> setAssignments = new List<SetAssignment>();
        foreach ((string columnName, string parameterName) in assignments)
        {
            setAssignments.Add(new SetParameterAssignment(columnName, parameterName));
        }
        Assignments = setAssignments;
    }

    public SetClause(IReadOnlyList<SetAssignment> assignments)
    {
        Assignments = assignments;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace()
            .WriteKeyword(emitter.Dialect.Set)
            .WriteSpace();

        for (int index = 0; index < Assignments.Count; index++)
        {
            SetAssignment assignment = Assignments[index];
            emitter.WriteIdentifier(assignment.ColumnName).WriteRaw(" = ");

            assignment.AcceptValue(emitter);

            if (index < Assignments.Count - 1)
                emitter.WriteRaw(", ");
        }
    }
}
