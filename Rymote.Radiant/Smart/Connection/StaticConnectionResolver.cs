using System.Data;

namespace Rymote.Radiant.Smart.Connection;

public class StaticConnectionResolver : IConnectionResolver
{
    private readonly IDbConnection _connection;
    
    public StaticConnectionResolver(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public IDbConnection GetConnection() => _connection;
}