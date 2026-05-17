using System;
using System.Data;

namespace Rymote.Radiant.Tests.TestSupport;

/// <summary>
/// A minimal stub IDbConnection that satisfies type checks during query construction.
/// Calling any data-access operation throws — use only with code paths that build SQL
/// in memory without executing it.
/// </summary>
internal sealed class OfflineDbConnection : IDbConnection
{
    public string ConnectionString { get; set; } = "offline";
    public int ConnectionTimeout => 0;
    public string Database => "offline";
    public ConnectionState State => ConnectionState.Closed;

    public IDbTransaction BeginTransaction() => throw new NotSupportedException();
    public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
    public void ChangeDatabase(string databaseName) => throw new NotSupportedException();
    public void Close() { }
    public IDbCommand CreateCommand() => throw new NotSupportedException();
    public void Open() => throw new NotSupportedException();
    public void Dispose() { }
}
