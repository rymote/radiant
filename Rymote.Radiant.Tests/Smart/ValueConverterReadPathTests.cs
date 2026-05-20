using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Mapping;
using Rymote.Radiant.Tests.TestModels;
using Xunit;

namespace Rymote.Radiant.Tests.Smart;

/// <summary>
/// Verifies that a registered <see cref="ValueConverter"/> is applied during result mapping when
/// the source-generated mapper is materializing a row. The Dapper-based read path consults
/// Dapper's <c>SqlMapper.TypeHandler</c>; the source-gen path must consult its own runtime
/// registry. Without this fix the generated mapper calls
/// <c>DbDataReader.GetFieldValue&lt;ContactId&gt;(columnIndex)</c> directly, which the provider
/// has no way to resolve.
/// </summary>
public sealed class ValueConverterReadPathTests
{
    [Fact]
    public async Task GeneratedMapperAppliesRegisteredValueConverterForStronglyTypedId()
    {
        ValueConverterRuntimeRegistry.Register(
            typeof(ContactId),
            new ValueConverter<ContactId, string>(
                toDatabase: id => id.Value,
                fromDatabase: rawValue => new ContactId(rawValue)));

        await using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (SqliteCommand schemaCommand = connection.CreateCommand())
        {
            schemaCommand.CommandText =
                "CREATE TABLE orders_with_strong_id (id TEXT PRIMARY KEY, status TEXT); " +
                "INSERT INTO orders_with_strong_id (id, status) VALUES ('01HMCONVERTED', 'shipped');";
            await schemaCommand.ExecuteNonQueryAsync();
        }

        bool mapperFound = SourceGeneratedMapperRegistry.TryGet(
            typeof(OrderWithStronglyTypedId),
            out Func<DbDataReader, object>? mapper);

        Assert.True(mapperFound, "Source-generated mapper should be registered for OrderWithStronglyTypedId.");
        Assert.NotNull(mapper);

        await using (SqliteCommand selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = "SELECT id, status FROM orders_with_strong_id";
            await using DbDataReader reader = await selectCommand.ExecuteReaderAsync();
            bool advanced = await reader.ReadAsync();
            Assert.True(advanced);

            OrderWithStronglyTypedId materializedOrder = (OrderWithStronglyTypedId)mapper!(reader);

            Assert.Equal(new ContactId("01HMCONVERTED"), materializedOrder.Id);
            Assert.Equal("shipped", materializedOrder.Status);
        }
    }
}
