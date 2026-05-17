using System.Collections.Generic;

namespace Rymote.Radiant.Adapters;

public sealed class CompiledQuery
{
    public string Sql { get; }
    public IReadOnlyList<QueryParameter> Parameters { get; }

    public CompiledQuery(string sql, IReadOnlyList<QueryParameter> parameters)
    {
        Sql = sql;
        Parameters = parameters;
    }
}
