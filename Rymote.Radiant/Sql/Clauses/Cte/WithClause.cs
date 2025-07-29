using System.Text;
using Rymote.Radiant.Sql.Dialects;
using Rymote.Radiant.Sql.Parameters;

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

    public void AppendTo(StringBuilder stringBuilder, ParameterBag parameterBag)
    {
        if (ctes.Count == 0) return;

        stringBuilder.Append(SqlKeywords.WITH).Append(SqlKeywords.SPACE);
        
        if (hasRecursiveCte)
            stringBuilder.Append(SqlKeywords.RECURSIVE).Append(SqlKeywords.SPACE);

        for (int index = 0; index < ctes.Count; index++)
        {
            if (index > 0) 
                stringBuilder.Append(SqlKeywords.COMMA);
            
            ctes[index].AppendTo(stringBuilder, parameterBag);
        }

        stringBuilder.Append(SqlKeywords.SPACE);
    }

    public bool HasCtes => ctes.Count > 0;
}