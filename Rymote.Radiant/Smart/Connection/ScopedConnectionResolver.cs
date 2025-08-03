using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Rymote.Radiant.Smart.Connection;

public class ScopedConnectionResolver : IConnectionResolver
{
    private readonly IServiceProvider serviceProvider;
    
    public ScopedConnectionResolver(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
    
    public IDbConnection GetConnection()
    {
        return serviceProvider.GetService<IDbConnection>() 
               ?? throw new InvalidOperationException("No IDbConnection registered in the service container");
    }
}