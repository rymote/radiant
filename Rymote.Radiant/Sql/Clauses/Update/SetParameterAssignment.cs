using System.Text;
using Rymote.Radiant.Sql.Dialects;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class SetParameterAssignment : SetAssignment
{
    public string ParameterName { get; }
    
    public SetParameterAssignment(string columnName, string parameterName) : base(columnName)
    {
        ParameterName = parameterName;
    }
    
    public override void AppendValueTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append(SqlKeywords.PARAMETER_PREFIX).Append(ParameterName);
    }
}