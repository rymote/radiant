using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Smart.Configuration;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartModel(this IServiceCollection services, Action<ISmartModelConfiguration> configure)
    {
        services.AddSingleton<IModelMetadataScanner, ModelMetadataScanner>();
        services.AddSingleton<IModelMetadataCache, ModelMetadataCache>();

        SmartModelConfiguration configuration = new SmartModelConfiguration();
        configure(configuration);
        configuration.Build();

        return services;
    }
}