using System.Data;

namespace Rymote.Radiant.Smart.Connection;

public interface IConnectionResolver
{
    IDbConnection GetConnection();
}