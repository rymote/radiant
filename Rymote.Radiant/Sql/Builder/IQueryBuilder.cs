using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql.Builder;

public interface IQueryBuilder
{
    /// <summary>
    /// Adapter-aware compilation. The dialect, identifier quoter, parameter formatter, and value
    /// writer all come from <paramref name="adapter"/>. This is the canonical compile entry point.
    /// </summary>
    QueryCommand Build(IDatabaseAdapter adapter);

    /// <summary>
    /// Legacy parameterless compile. Routes through the static <c>QueryCompiler.Compile</c> which
    /// emits PostgreSQL-flavoured SQL via the legacy <c>AppendTo</c> path. Will be removed once
    /// every internal caller passes an adapter.
    /// </summary>
    QueryCommand Build();
}
