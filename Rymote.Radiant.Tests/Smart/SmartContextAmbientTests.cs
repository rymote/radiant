using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Adapters.PostgreSql;
using Rymote.Radiant.Adapters.PostgreSql.DependencyInjection;
using Rymote.Radiant.Smart.Context;
using Rymote.Radiant.Smart.DependencyInjection;
using Rymote.Radiant.Tests.TestModels;
using Xunit;

namespace Rymote.Radiant.Tests.Smart;

public sealed class SmartContextAmbientTests
{
    [Fact]
    public void AmbientContextCanBeSetAndCleared()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRadiant(builder =>
        {
            builder.UsePostgreSql("Host=localhost;Database=neverConnected");
            builder.RegisterModel<TestUser>();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        SmartContext context = scope.ServiceProvider.GetRequiredService<SmartContext>();

        Assert.Null(SmartContextAmbient.CurrentOrNull);

        using (SmartContextAmbient.Use(context))
        {
            Assert.Same(context, SmartContextAmbient.Current);
        }

        Assert.Null(SmartContextAmbient.CurrentOrNull);
    }

    [Fact]
    public void RadiantBuilderRegistersPostgreSqlAdapter()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddRadiant(builder =>
        {
            builder.UsePostgreSql("Host=localhost;Database=neverConnected");
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        SmartContext context = scope.ServiceProvider.GetRequiredService<SmartContext>();

        Assert.IsType<PostgreSqlAdapter>(context.Adapter);
        Assert.Equal("postgresql", context.Adapter.Identifier);
    }
}
