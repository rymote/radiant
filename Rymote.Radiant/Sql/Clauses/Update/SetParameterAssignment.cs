using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Update;

public sealed class SetParameterAssignment : SetAssignment
{
    public string ParameterName { get; }

    public SetParameterAssignment(string columnName, string parameterName) : base(columnName)
    {
        ParameterName = parameterName;
    }

    public override void AcceptValue(SqlEmitter emitter)
    {
        emitter.WriteRaw("@").WriteRaw(ParameterName);
    }
}
