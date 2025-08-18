using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Rymote.Radiant.Smart.Connection;

public class ScopedConnectionResolver : IConnectionResolver
{
    private readonly IServiceProvider _serviceProvider;
    
    public ScopedConnectionResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IDbConnection GetConnection()
    {
        return _serviceProvider.GetService<IDbConnection>() 
               ?? throw new InvalidOperationException("No IDbConnection registered in the service container");
    }
}