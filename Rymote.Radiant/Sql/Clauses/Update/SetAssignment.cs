using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Update;

public abstract class SetAssignment
{
    public string ColumnName { get; }

    protected SetAssignment(string columnName)
    {
        ColumnName = columnName;
    }

    public abstract void AcceptValue(SqlEmitter emitter);
}
