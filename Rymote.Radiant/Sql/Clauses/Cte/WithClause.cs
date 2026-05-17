using Rymote.Radiant.Sql.Compiler;

namespace Rymote.Radiant.Sql.Clauses.Cte;

public sealed class WithClause : IQueryClause
{
    private readonly List<CteClause> ctes = new();
    private bool hasRecursiveCte;

    public void AddCte(CteClause cte)
    {
        ctes.Add(cte);
        if (cte.IsRecursive)
            hasRecursiveCte = true;
    }

    public bool HasCtes => ctes.Count > 0;

    public void Accept(SqlEmitter emitter)
    {
        if (ctes.Count == 0) return;

        emitter.WriteKeyword(emitter.Dialect.With).WriteSpace();

        if (hasRecursiveCte)
            emitter.WriteKeyword(emitter.Dialect.Recursive).WriteSpace();

        for (int index = 0; index < ctes.Count; index++)
        {
            if (index > 0)
                emitter.WriteRaw(", ");

            emitter.Emit(ctes[index]);
        }

        emitter.WriteSpace();
    }
}
