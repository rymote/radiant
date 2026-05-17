using System;
using System.Collections.Generic;
using Rymote.Radiant.Adapters;
using Rymote.Radiant.Smart.Metadata;

namespace Rymote.Radiant.Smart.Configuration;

public sealed class SmartContextOptions
{
    public IDatabaseAdapter Adapter { get; }
    public IModelMetadataCache ModelMetadataCache { get; }
    public IReadOnlyDictionary<Type, ValueConverter> ValueConverters { get; }
    public IReadOnlyList<GlobalQueryFilter> GlobalQueryFilters { get; }
    public string? SchemaOverride { get; }
    public int CommandTimeoutSeconds { get; }

    public SmartContextOptions(
        IDatabaseAdapter adapter,
        IModelMetadataCache modelMetadataCache,
        IReadOnlyDictionary<Type, ValueConverter> valueConverters,
        IReadOnlyList<GlobalQueryFilter> globalQueryFilters,
        string? schemaOverride = null,
        int commandTimeoutSeconds = 30)
    {
        Adapter = adapter;
        ModelMetadataCache = modelMetadataCache;
        ValueConverters = valueConverters;
        GlobalQueryFilters = globalQueryFilters;
        SchemaOverride = schemaOverride;
        CommandTimeoutSeconds = commandTimeoutSeconds;
    }

    public SmartContextOptions WithSchema(string schemaName)
        => new SmartContextOptions(Adapter, ModelMetadataCache, ValueConverters, GlobalQueryFilters, schemaName, CommandTimeoutSeconds);
}
