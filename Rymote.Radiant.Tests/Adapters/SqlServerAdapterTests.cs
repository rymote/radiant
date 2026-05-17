using System;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.SqlServer;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Adapters;

/// <summary>
/// Unit-level coverage for the SQL Server adapter: identifier quoting, parameter formatting,
/// capability flags, and the explicit NotSupportedException surface on dialect features that
/// SQL Server expresses differently from PostgreSQL.
/// </summary>
public sealed class SqlServerAdapterTests
{
    private static SqlServerAdapter Adapter { get; } = new SqlServerAdapter("Server=offline;Database=offline;Integrated Security=true;TrustServerCertificate=true");

    [Fact]
    public void SqlServerEmitsBracketQuotedIdentifiersAndAtPrefixedParameters()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("users")
            .Where("id", "=", 42);

        string sqlServerSql = selectBuilder.Build(Adapter).SqlText;

        Assert.Contains("[users]", sqlServerSql);
        Assert.Contains("@p0", sqlServerSql);
        Assert.DoesNotContain("\"users\"", sqlServerSql);
    }

    [Fact]
    public void SqlServerUsesAtPlaceholderAndDoesNotAdvertiseUpsertOnConflict()
    {
        Assert.Equal("@p0", Adapter.ParameterFormatter.FormatPlaceholder(0));
        Assert.Equal(DatabaseCapabilities.None, Adapter.Capabilities & DatabaseCapabilities.UpsertOnConflict);
    }

    [Fact]
    public void SqlServerDialectOnConflictThrowsAndMentionsMerge()
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => _ = Adapter.Dialect.OnConflict);
        Assert.Contains("MERGE", exception.Message);
    }
}
