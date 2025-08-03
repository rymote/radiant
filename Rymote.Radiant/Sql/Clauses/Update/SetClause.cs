using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.SET)
            .Append(SqlKeywords.SPACE);
        
        for (int index = 0; index < Assignments.Count; index++)
        {
            SetAssignment assignment = Assignments[index];
            stringBuilder
                .Append(SqlKeywords.QUOTE)
                .Append(assignment.ColumnName)
                .Append(SqlKeywords.QUOTE)
                .Append(SqlKeywords.EQUALS);
                
            assignment.AppendValueTo(stringBuilder);
            
            if (index < Assignments.Count - 1) 
                stringBuilder.Append(SqlKeywords.COMMA);
        }
    }
}