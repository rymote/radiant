using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class ValuesClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }
    public IReadOnlyList<string> ParameterNames { get; }

    public ValuesClause(IReadOnlyList<string> columnNames, IReadOnlyList<string> parameterNames)
    {
        ColumnNames = columnNames;
        ParameterNames = parameterNames;
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteSpace().WriteRaw("(");
        for (int index = 0; index < ColumnNames.Count; index++)
        {
            emitter.WriteIdentifier(ColumnNames[index]);
            if (index < ColumnNames.Count - 1)
                emitter.WriteRaw(", ");
        }

        emitter.WriteRaw(")")
            .WriteSpace()
            .WriteKeyword(emitter.Dialect.Values)
            .WriteSpace()
            .WriteRaw("(");

        for (int index = 0; index < ParameterNames.Count; index++)
        {
            emitter.WriteRaw("@").WriteRaw(ParameterNames[index]);
            if (index < ParameterNames.Count - 1) emitter.WriteRaw(", ");
        }

        emitter.WriteRaw(")");
    }
}
