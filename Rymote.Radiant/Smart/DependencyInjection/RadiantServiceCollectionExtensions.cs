using System;
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Context;

namespace Rymote.Radiant.Smart.DependencyInjection;

public static class RadiantServiceCollectionExtensions
{
    public static IServiceCollection AddRadiant(this IServiceCollection services, Action<RadiantBuilder> configureBuilder)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configureBuilder is null) throw new ArgumentNullException(nameof(configureBuilder));

        RadiantBuilder radiantBuilder = new RadiantBuilder(services);
        configureBuilder(radiantBuilder);

        services.AddSingleton<RadiantBuilder>(radiantBuilder);
        services.AddSingleton<SmartContextOptions>(serviceProvider => radiantBuilder.BuildOptions(serviceProvider));
        services.AddScoped<SmartContext>(serviceProvider =>
        {
            SmartContextOptions options = serviceProvider.GetRequiredService<SmartContextOptions>();
            return new SmartContext(options, serviceProvider);
        });

        return services;
    }
}
