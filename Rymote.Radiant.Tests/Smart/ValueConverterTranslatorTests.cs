using System.Collections.Generic;
using Npgsql;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Expressions;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Tests.TestModels;
using Xunit;

namespace Rymote.Radiant.Tests.Smart;

/// <summary>
/// Verifies that <see cref="LinqPredicateTranslator"/> applies registered
/// <see cref="ValueConverter"/>s to captured values before they reach the parameter bag — so a
/// strongly-typed ID (<see cref="ContactId"/>) emits its underlying string, not its CLR shape.
/// </summary>
public sealed class ValueConverterTranslatorTests
{
    private static IModelMetadata TestUserMetadata { get; } = LoadMetadata();
    private static IDatabaseAdapter PostgresAdapter { get; } = BuildOfflinePostgresAdapter();

    private static IDatabaseAdapter BuildOfflinePostgresAdapter()
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=offline;Database=offline");
        return new PostgreSqlAdapter(dataSourceBuilder.Build());
    }

    private static Dictionary<System.Type, ValueConverter> Converters { get; } = new()
    {
        [typeof(ContactId)] = new ValueConverter<ContactId, string>(
            toDatabase: contactId => contactId.Value,
            fromDatabase: rawValue => new ContactId(rawValue))
    };

    private static IModelMetadata LoadMetadata()
    {
        ModelMetadataScanner scanner = new ModelMetadataScanner();
        ModelMetadataCache cache = new ModelMetadataCache(scanner);
        cache.RegisterModel<TestUser>();
        return cache.GetMetadata<TestUser>();
    }

    [Fact]
    public void RegisteredConverterUnwrapsStrongIdToUnderlyingValueAtParameterBindTime()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From("contacts");

        LinqPredicateTranslator translator = new LinqPredicateTranslator(TestUserMetadata, Converters);

        // Predicate uses a captured ContactId compared against a string property.
        // The translator must invoke the ContactId -> string converter so the bound parameter
        // is the plain string "01HMEXAMPLE", not a boxed ContactId struct.
        ContactId targetId = new ContactId("01HMEXAMPLE");
        translator.TranslateInto(
            (System.Linq.Expressions.Expression<System.Func<TestUser, bool>>)(user => user.Username == targetId.Value),
            selectBuilder);

        Rymote.Radiant.Sql.QueryCommand compiled = selectBuilder.Build(PostgresAdapter);
        Assert.Contains("\"username\" = @p0", compiled.SqlText);
        Assert.Single(compiled.OrderedParameters);
        Assert.Equal("01HMEXAMPLE", compiled.OrderedParameters[0].Value);
    }

    [Fact]
    public void TranslatorWithoutConvertersPassesValuesUnchanged()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From("users");

        LinqPredicateTranslator translator = new LinqPredicateTranslator(TestUserMetadata);

        int targetId = 42;
        translator.TranslateInto(
            (System.Linq.Expressions.Expression<System.Func<TestUser, bool>>)(user => user.Id == targetId),
            selectBuilder);

        Rymote.Radiant.Sql.QueryCommand compiled = selectBuilder.Build(PostgresAdapter);
        Assert.Equal(42, compiled.OrderedParameters[0].Value);
    }
}
