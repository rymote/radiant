using System.Data;
using Npgsql;

namespace Rymote.Radiant.Core;

public abstract class RadiantDbContext : IDisposable, IAsyncDisposable
{
    private readonly string connectionString;
    private NpgsqlConnection? connection;
    private NpgsqlTransaction? transaction;

    protected RadiantDbContext(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public IDbConnection Connection
    {
        get
        {
            if (connection == null)
            {
                connection = new NpgsqlConnection(connectionString);
                connection.Open();
            }
            return connection;
        }
    }

    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        if (connection == null)
        {
            connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
        }
        transaction = await connection.BeginTransactionAsync();
        return transaction;
    }

    public void Dispose()
    {
        transaction?.Dispose();
        connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
        if (connection != null)
            await connection.DisposeAsync();
    }
}
