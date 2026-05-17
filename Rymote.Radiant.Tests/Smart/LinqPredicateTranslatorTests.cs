using System.Linq;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Expressions;
using Rymote.Radiant.Smart.Metadata;
using Rymote.Radiant.Sql.Builder;
using Rymote.Radiant.Sql.Expressions;
using Rymote.Radiant.Tests.TestModels;
using Xunit;

namespace Rymote.Radiant.Tests.Smart;

public sealed class LinqPredicateTranslatorTests
{
    private static IModelMetadata TestUserMetadata { get; } = LoadMetadata();

    private static IModelMetadata LoadMetadata()
    {
        ModelMetadataScanner scanner = new ModelMetadataScanner();
        ModelMetadataCache cache = new ModelMetadataCache(scanner);
        cache.RegisterModel<TestUser>();
        return cache.GetMetadata<TestUser>();
    }

    private static string CompileSelect(System.Linq.Expressions.Expression<System.Func<TestUser, bool>> predicate)
    {
        SelectBuilder selectBuilder = new SelectBuilder()
            .Select(new RawSqlExpression("*"))
            .From("users");

        LinqPredicateTranslator translator = new LinqPredicateTranslator(TestUserMetadata);
        translator.TranslateInto(predicate, selectBuilder);

        return selectBuilder.Build().SqlText;
    }

    [Fact]
    public void SingleEqualityPredicateEmitsEqualsClause()
    {
        string sql = CompileSelect(user => user.Id == 5);
        Assert.Contains("\"id\" = @p0", sql);
    }

    [Fact]
    public void AndAlsoCombinesPredicates()
    {
        string sql = CompileSelect(user => user.Id == 5 && user.IsActive);
        Assert.Contains("\"id\" = @p0", sql);
        Assert.Contains("AND", sql);
        Assert.Contains("\"is_active\" = @p1", sql);
    }

    [Fact]
    public void StringContainsEmitsLikeWithSurroundingWildcards()
    {
        string sql = CompileSelect(user => user.Email.Contains("@example.com"));
        Assert.Contains("\"email\" LIKE @p0", sql);
    }

    [Fact]
    public void StringStartsWithEmitsLikeWithTrailingWildcard()
    {
        string sql = CompileSelect(user => user.Username.StartsWith("admin"));
        Assert.Contains("\"username\" LIKE @p0", sql);
    }

    [Fact]
    public void EnumerableContainsEmitsInClause()
    {
        int[] candidateIds = new[] { 1, 2, 3 };
        string sql = CompileSelect(user => candidateIds.Contains(user.Id));
        Assert.Contains("\"id\" IN", sql);
    }

    [Fact]
    public void EqualNullEmitsIsNull()
    {
        string sql = CompileSelect(user => user.Email == null);
        Assert.Contains("\"email\" IS NULL", sql);
    }

    [Fact]
    public void NotEqualNullEmitsIsNotNull()
    {
        string sql = CompileSelect(user => user.Email != null);
        Assert.Contains("\"email\" IS NOT NULL", sql);
    }

    [Fact]
    public void GreaterThanComparisonWorksWithCapturedVariable()
    {
        int minimumAge = 18;
        string sql = CompileSelect(user => user.Age >= minimumAge);
        Assert.Contains("\"age\" >= @p0", sql);
    }

    [Fact]
    public void BooleanPropertyAccessEmitsEqualsTrue()
    {
        string sql = CompileSelect(user => user.IsActive);
        Assert.Contains("\"is_active\" = @p0", sql);
    }
}
