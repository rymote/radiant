using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Rymote.Radiant.Adapters;

public interface IDatabaseAdapter
{
    string Identifier { get; }
    DatabaseCapabilities Capabilities { get; }
    ISqlDialect Dialect { get; }
    IIdentifierQuoter IdentifierQuoter { get; }
    IParameterFormatter ParameterFormatter { get; }
    IValueWriter ValueWriter { get; }
    IResultMapper ResultMapper { get; }

    DbConnection CreateConnection();
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
    DbCommand CreateCommand(DbConnection connection, CompiledQuery compiledQuery);
}
