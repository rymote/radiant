using Rymote.Radiant.Adapters;

namespace Rymote.Radiant.Sql.Builder;

public interface IQueryBuilder
{
    /// <summary>
    /// Adapter-aware compilation. The dialect, identifier quoter, parameter formatter, and value
    /// writer all come from <paramref name="adapter"/>. This is the canonical compile entry point.
    /// </summary>
    QueryCommand Build(IDatabaseAdapter adapter);
}
