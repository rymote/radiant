using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;

namespace Rymote.Radiant.Smart.Mapping;

/// <summary>
/// Process-global registry of source-generated <see cref="DbDataReader"/>-to-CLR mappers. Each
/// generated mapper class self-registers via a <c>[ModuleInitializer]</c> at module load time, so
/// no manual wiring is required by the consumer — registration is automatic for every
/// <see cref="SmartModel{TModel}"/> in any loaded assembly that ships generated mappers.
///
/// When a generated mapper is present for a given CLR type, <see cref="Rymote.Radiant.Sql.Executor.QueryExecutor"/>
/// uses it for result materialization in preference to the adapter's Dapper-backed reflection mapper.
/// Source-generated mappers are zero-allocation in the hot path and AOT-friendly.
/// </summary>
public static class SourceGeneratedMapperRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<DbDataReader, object>> mappersByClrType = new();

    public static void Register<TModel>(Func<DbDataReader, TModel> mapper) where TModel : class
    {
        if (mapper is null) throw new ArgumentNullException(nameof(mapper));
        mappersByClrType[typeof(TModel)] = reader => mapper(reader)!;
    }

    public static bool TryGet<TModel>(out Func<DbDataReader, TModel>? mapper) where TModel : class
    {
        if (mappersByClrType.TryGetValue(typeof(TModel), out Func<DbDataReader, object>? rawMapper))
        {
            mapper = reader => (TModel)rawMapper(reader);
            return true;
        }

        mapper = null;
        return false;
    }

    /// <summary>
    /// Non-generic try-get for callers that constrain via runtime type-checking rather than the
    /// generic <c>where TModel : class</c> constraint (for example <see cref="Rymote.Radiant.Sql.Executor.QueryExecutor"/>,
    /// which is invoked with any TResult including non-reference types).
    /// </summary>
    public static bool TryGet(Type clrType, out Func<DbDataReader, object>? mapper)
    {
        if (mappersByClrType.TryGetValue(clrType, out Func<DbDataReader, object>? rawMapper))
        {
            mapper = rawMapper;
            return true;
        }

        mapper = null;
        return false;
    }

    public static IReadOnlyDictionary<Type, Func<DbDataReader, object>> RegisteredMappers => mappersByClrType;
}
