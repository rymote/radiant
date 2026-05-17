using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Insert;

public sealed class ValuesExpressionClause : IQueryClause
{
    public IReadOnlyList<string> ColumnNames { get; }
    public IReadOnlyList<ISqlExpression> Expressions { get; }

    public ValuesExpressionClause(IReadOnlyList<string> columnNames, IReadOnlyList<ISqlExpression> expressions)
    {
        ColumnNames = columnNames;
        Expressions = expressions;
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

        for (int index = 0; index < Expressions.Count; index++)
        {
            emitter.Emit(Expressions[index]);
            if (index < Expressions.Count - 1)
                emitter.WriteRaw(", ");
        }

        emitter.WriteRaw(")");
    }
}
