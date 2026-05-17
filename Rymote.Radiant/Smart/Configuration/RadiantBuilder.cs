using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class RadiantBuilder
{
    private readonly IServiceCollection services;
    private readonly Dictionary<Type, ValueConverter> valueConverters = new();
    private readonly List<GlobalQueryFilter> globalQueryFilters = new();
    private readonly ModelMetadataScanner metadataScanner = new();
    private readonly ModelMetadataCache metadataCache;
    private Func<IServiceProvider, IDatabaseAdapter>? adapterFactory;
    private int commandTimeoutSeconds = 30;

    public RadiantBuilder(IServiceCollection services)
    {
        this.services = services;
        metadataCache = new ModelMetadataCache(metadataScanner);
    }

    public IServiceCollection Services => services;
    public IModelMetadataCache MetadataCache => metadataCache;

    public RadiantBuilder UseAdapter(Func<IServiceProvider, IDatabaseAdapter> factory)
    {
        adapterFactory = factory;
        return this;
    }

    public RadiantBuilder RegisterModel<TModel>() where TModel : class
    {
        metadataCache.RegisterModel<TModel>();
        return this;
    }

    public RadiantBuilder RegisterModel(Type modelType)
    {
        metadataCache.RegisterModel(modelType);
        return this;
    }

    public RadiantBuilder RegisterModelsFromAssembly(Assembly assembly)
    {
        foreach (Type modelType in assembly.GetTypes())
        {
            if (modelType.IsAbstract || modelType.IsInterface) continue;
            if (typeof(SmartModel).IsAssignableFrom(modelType))
                metadataCache.RegisterModel(modelType);
        }
        return this;
    }

    public RadiantBuilder AddValueConverter<TClr, TDatabase>(Func<TClr, TDatabase> toDatabase, Func<TDatabase, TClr> fromDatabase)
    {
        valueConverters[typeof(TClr)] = new ValueConverter<TClr, TDatabase>(toDatabase, fromDatabase);
        return this;
    }

    public RadiantBuilder AddGlobalQueryFilter(GlobalQueryFilter filter)
    {
        globalQueryFilters.Add(filter);
        return this;
    }

    public RadiantBuilder WithCommandTimeout(int seconds)
    {
        commandTimeoutSeconds = seconds;
        return this;
    }

    internal SmartContextOptions BuildOptions(IServiceProvider serviceProvider)
    {
        if (adapterFactory is null)
            throw new InvalidOperationException(
                "No database adapter configured. Call UsePostgreSql(...) or another adapter extension before AddRadiant completes.");

        IDatabaseAdapter adapter = adapterFactory(serviceProvider);
        return new SmartContextOptions(
            adapter,
            metadataCache,
            valueConverters,
            globalQueryFilters,
            schemaOverride: null,
            commandTimeoutSeconds);
    }
}
