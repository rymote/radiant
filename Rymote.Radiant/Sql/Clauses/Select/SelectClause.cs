using System.Text;
using Rymote.Radiant.Sql.Compiler;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Sql.Parameters;

namespace Rymote.Radiant.Sql.Clauses.Select;

public sealed class SelectClause : IQueryClause
{
    public IReadOnlyList<ISqlExpression> Expressions { get; }

    public SelectClause(IEnumerable<ISqlExpression> expressions)
    {
        Expressions = expressions.ToList();
    }

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        stringBuilder.Append(SqlKeywords.SELECT).Append(SqlKeywords.SPACE);

        for (int index = 0; index < Expressions.Count; index++)
        {
            Expressions[index].AppendTo(stringBuilder);
            if (index < Expressions.Count - 1) stringBuilder.Append(SqlKeywords.COMMA);
        }

        stringBuilder.Append(SqlKeywords.SPACE);
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