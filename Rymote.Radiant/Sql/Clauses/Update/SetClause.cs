using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class SetClause : IQueryClause
{
    public IReadOnlyList<(string ColumnName, string ParameterName)> Assignments { get; }
    public SetClause(IReadOnlyList<(string ColumnName, string ParameterName)> assignments) =>
        Assignments = assignments;

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder
            .Append(SqlKeywords.SPACE)
            .Append(SqlKeywords.SET)
            .Append(SqlKeywords.SPACE);
        
        for (int index = 0; index < Assignments.Count; index++)
        {
            (string columnName, string parameterName) = Assignments[index];
            stringBuilder
                .Append(SqlKeywords.QUOTE)
                .Append(columnName)
                .Append(SqlKeywords.QUOTE)
                .Append(SqlKeywords.EQUALS)
                .Append(SqlKeywords.PARAMETER_PREFIX)
                .Append(parameterName);
            
            if (index < Assignments.Count - 1) stringBuilder.Append(SqlKeywords.COMMA);
        }
    }
}
