using System.Data;

namespace Rymote.Radiant.Smart.Connection;

public class StaticConnectionResolver : IConnectionResolver
{
    private readonly IDbConnection connection;
    
    public StaticConnectionResolver(IDbConnection connection)
    {
        this.connection = connection;
    }
    
    public IDbConnection GetConnection() => connection;
}