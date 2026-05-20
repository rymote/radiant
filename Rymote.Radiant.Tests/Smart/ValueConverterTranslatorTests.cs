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
/// The translator is the write-side counterpart to the source-generated mapper's read-side
/// runtime registry; both must apply the same converters.
/// </summary>
public sealed class ValueConverterTranslatorTests
{
    private static IModelMetadata OrderWithStrongIdMetadata { get; } = LoadOrderMetadata();
    private static IModelMetadata TestUserMetadata { get; } = LoadTestUserMetadata();
    private static IDatabaseAdapter PostgresAdapter { get; } = BuildOfflinePostgresAdapter();

    private static IDatabaseAdapter BuildOfflinePostgresAdapter()
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=offline;Database=offline");
        return new PostgreSqlAdapter(dataSourceBuilder.Build());
    }

    private static Dictionary<System.Type, ValueConverter> ContactIdConverters { get; } = new()
    {
        [typeof(ContactId)] = new ValueConverter<ContactId, string>(
            toDatabase: contactId => contactId.Value,
            fromDatabase: rawValue => new ContactId(rawValue))
    };

    private static IModelMetadata LoadOrderMetadata()
    {
        ModelMetadataScanner scanner = new ModelMetadataScanner();
        ModelMetadataCache cache = new ModelMetadataCache(scanner);
        cache.RegisterModel<OrderWithStronglyTypedId>();
        return cache.GetMetadata<OrderWithStronglyTypedId>();
    }

    private static IModelMetadata LoadTestUserMetadata()
    {
        ModelMetadataScanner scanner = new ModelMetadataScanner();
        ModelMetadataCache cache = new ModelMetadataCache(scanner);
        cache.RegisterModel<TestUser>();
        return cache.GetMetadata<TestUser>();
    }

    [Fact]
    public void RegisteredConverterUnwrapsStrongIdValueWhenComparedDirectly()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From("orders_with_strong_id");

        LinqPredicateTranslator translator = new LinqPredicateTranslator(OrderWithStrongIdMetadata, ContactIdConverters);

        // The captured value is a ContactId struct — NOT contactId.Value. This is the real test of
        // converter application: if the converter is wired correctly, the bound parameter must be
        // the underlying string "01HMEXAMPLE", not a boxed ContactId instance.
        ContactId targetId = new ContactId("01HMEXAMPLE");
        translator.TranslateInto(
            (System.Linq.Expressions.Expression<System.Func<OrderWithStronglyTypedId, bool>>)(order => order.Id == targetId),
            selectBuilder);

        Rymote.Radiant.Sql.QueryCommand compiled = selectBuilder.Build(PostgresAdapter);
        Assert.Contains("\"id\" = @p0", compiled.SqlText);
        Assert.Single(compiled.OrderedParameters);
        Assert.Equal("01HMEXAMPLE", compiled.OrderedParameters[0].Value);
        Assert.IsType<string>(compiled.OrderedParameters[0].Value);
    }

    [Fact]
    public void TranslatorWithoutConvertersBindsCapturedStrongIdAsBoxedStruct()
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From("orders_with_strong_id");

        LinqPredicateTranslator translator = new LinqPredicateTranslator(OrderWithStrongIdMetadata);

        ContactId targetId = new ContactId("01HMNOCONVERTER");
        translator.TranslateInto(
            (System.Linq.Expressions.Expression<System.Func<OrderWithStronglyTypedId, bool>>)(order => order.Id == targetId),
            selectBuilder);

        Rymote.Radiant.Sql.QueryCommand compiled = selectBuilder.Build(PostgresAdapter);
        Assert.Single(compiled.OrderedParameters);
        // Without a converter the translator binds the raw ContactId — provider-level failure is on
        // the consumer; this asserts the no-converter path is the documented passthrough.
        Assert.IsType<ContactId>(compiled.OrderedParameters[0].Value);
    }

    [Fact]
    public void TranslatorWithoutConvertersPassesPrimitiveValuesUnchanged()
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
