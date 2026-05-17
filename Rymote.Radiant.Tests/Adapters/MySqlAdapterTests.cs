using System;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.MySql;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Xunit;

namespace Rymote.Radiant.Tests.Adapters;

/// <summary>
/// Compile-only tests for the MySQL adapter. They verify that the same builder graph compiles to
/// backtick-quoted identifiers and '@pN' placeholders under the MySQL dialect, and that
/// MySQL-unsupported dialect properties throw NotSupportedException.
/// </summary>
public sealed class MySqlAdapterTests
{
    private static MySqlAdapter Adapter { get; } = new MySqlAdapter("Server=offline;Database=offline;Uid=offline;Pwd=offline;");

    [Fact]
    public void MySqlCompilesSelectWithBacktickQuotedIdentifiersAndAtPlaceholder()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new ColumnExpression("id"))
            .From("users")
            .Where("id", "=", 42);

        string compiledSql = selectBuilder.Build(Adapter).SqlText;

        Assert.Contains("`users`", compiledSql);
        Assert.Contains("`id`", compiledSql);
        Assert.Contains("@p0", compiledSql);
    }

    [Fact]
    public void MySqlParameterFormatterFormatsPlaceholderAsAtPZero()
    {
        Assert.Equal("@p0", Adapter.ParameterFormatter.FormatPlaceholder(0));
        Assert.Equal("p0", Adapter.ParameterFormatter.FormatParameterName(0));
    }

    [Fact]
    public void MySqlCapabilitiesDoNotIncludeUpsertOrReturningOrJsonbOrArrayOrVectorOrRange()
    {
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.UpsertOnConflict));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.ReturningClause));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.JsonbColumn));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.ArrayColumn));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.VectorColumn));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.RangeTypes));
        Assert.False(Adapter.Capabilities.HasFlag(DatabaseCapabilities.LateralJoin));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.CommonTableExpression));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.WindowFunctions));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.FullTextSearch));
        Assert.True(Adapter.Capabilities.HasFlag(DatabaseCapabilities.CaseInsensitiveLikeOperator));
    }

    [Fact]
    public void MySqlDialectThrowsForUnsupportedConflictAndReturningKeywords()
    {
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.OnConflict);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.DoNothing);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.DoUpdate);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.ExcludedTableAlias);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.ReturningKeyword);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.ConcatenateOperator);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.CastOperator);
        Assert.Throws<NotSupportedException>(() => Adapter.Dialect.LateralKeyword);
    }
}
