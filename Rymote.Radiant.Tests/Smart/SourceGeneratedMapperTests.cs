using System.Data.Common;
using Rymote.Radiant.Smart.Mapping;
using Rymote.Radiant.Tests.TestModels;
using Xunit;

namespace Rymote.Radiant.Tests.Smart;

/// <summary>
/// Verifies that the SmartModelGenerator emits a per-model RadiantMapper class and that the
/// class's <c>[ModuleInitializer]</c> registers it with the global
/// <see cref="SourceGeneratedMapperRegistry"/> at module load time.
/// </summary>
public sealed class SourceGeneratedMapperTests
{
    [Fact]
    public void GeneratedMapperRegistersItselfForTestUser()
    {
        // Touch TestUser so its containing module's [ModuleInitializer] fires (if it hadn't yet).
        _ = typeof(TestUser).Name;

        bool found = SourceGeneratedMapperRegistry.TryGet(typeof(TestUser), out System.Func<DbDataReader, object>? mapper);
        Assert.True(found, "Expected source-generated mapper for TestUser to be registered.");
        Assert.NotNull(mapper);
    }

    [Fact]
    public void RegistryReturnsFalseForUnknownType()
    {
        bool found = SourceGeneratedMapperRegistry.TryGet(typeof(string), out System.Func<DbDataReader, object>? mapper);
        Assert.False(found);
        Assert.Null(mapper);
    }
}
