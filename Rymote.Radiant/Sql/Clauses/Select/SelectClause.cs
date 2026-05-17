using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Expressions;

namespace Rymote.Radiant.Sql.Clauses.Select;

public sealed class SelectClause : IQueryClause
{
    public IReadOnlyList<ISqlExpression> Expressions { get; }

    public SelectClause(IEnumerable<ISqlExpression> expressions)
    {
        Expressions = expressions.ToList();
    }

    public void Accept(SqlEmitter emitter)
    {
        emitter.WriteKeyword(emitter.Dialect.Select).WriteSpace();

        for (int index = 0; index < Expressions.Count; index++)
        {
            emitter.Emit(Expressions[index]);
            if (index < Expressions.Count - 1)
                emitter.WriteRaw(", ");
        }

        emitter.WriteSpace();
    }
}
