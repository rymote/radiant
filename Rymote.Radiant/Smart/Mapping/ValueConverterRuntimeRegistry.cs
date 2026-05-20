using System;
using System.Collections.Concurrent;
using System.Data.Common;
using Rymote.Radiant.Smart.Configuration;

namespace Rymote.Radiant.Smart.Mapping;

/// <summary>
/// Process-global registry of <see cref="ValueConverter"/>s keyed by CLR type. Populated by
/// <see cref="RadiantBuilder.BuildOptions"/> when <see cref="RadiantBuilder.AddValueConverter{TClr, TDatabase}"/>
/// has registered converters. Consulted by the source-generated result mappers at row-materialization time
/// to apply <c>FromDatabase</c> when reading a column whose CLR property type has a converter.
///
/// This sits alongside <see cref="ValueConverterDapperRegistry"/>: that one wires converters into Dapper's
/// reflection-based result mapper (consulted only when <c>QueryExecutor</c> falls back to Dapper). The
/// source-generated mappers run instead of Dapper for every model that has one, so they need their own
/// hook into the converter pipeline.
/// </summary>
public static class ValueConverterRuntimeRegistry
{
    private static readonly ConcurrentDictionary<Type, ValueConverter> registeredConverters = new();

    public static void Register(Type clrType, ValueConverter converter)
    {
        if (clrType is null) throw new ArgumentNullException(nameof(clrType));
        if (converter is null) throw new ArgumentNullException(nameof(converter));
        registeredConverters[clrType] = converter;
    }

    public static bool TryGet(Type clrType, out ValueConverter? converter)
    {
        if (registeredConverters.TryGetValue(clrType, out ValueConverter? found))
        {
            converter = found;
            return true;
        }

        converter = null;
        return false;
    }

    /// <summary>
    /// Used by source-generated mappers in place of <c>reader.GetFieldValue&lt;TClr&gt;</c> for
    /// non-primitive property types. Applies a registered <see cref="ValueConverter"/> when one
    /// exists; otherwise falls back to <see cref="DbDataReader.GetFieldValue{T}"/>.
    /// </summary>
    public static TClr Read<TClr>(DbDataReader reader, int columnIndex)
    {
        if (!registeredConverters.TryGetValue(typeof(TClr), out ValueConverter? converter))
            return reader.GetFieldValue<TClr>(columnIndex);

        object databaseValue = ReadDatabaseValue(reader, columnIndex, converter.DatabaseType);
        object? clrValue = converter.FromDatabase(databaseValue);
        return clrValue is null ? default! : (TClr)clrValue;
    }

    private static object ReadDatabaseValue(DbDataReader reader, int columnIndex, Type databaseType)
    {
        if (databaseType == typeof(string))         return reader.GetString(columnIndex);
        if (databaseType == typeof(int))            return reader.GetInt32(columnIndex);
        if (databaseType == typeof(long))           return reader.GetInt64(columnIndex);
        if (databaseType == typeof(short))          return reader.GetInt16(columnIndex);
        if (databaseType == typeof(byte))           return reader.GetByte(columnIndex);
        if (databaseType == typeof(bool))           return reader.GetBoolean(columnIndex);
        if (databaseType == typeof(decimal))        return reader.GetDecimal(columnIndex);
        if (databaseType == typeof(double))         return reader.GetDouble(columnIndex);
        if (databaseType == typeof(float))          return reader.GetFloat(columnIndex);
        if (databaseType == typeof(Guid))           return reader.GetGuid(columnIndex);
        if (databaseType == typeof(DateTime))       return reader.GetDateTime(columnIndex);
        if (databaseType == typeof(DateTimeOffset)) return reader.GetFieldValue<DateTimeOffset>(columnIndex);
        return reader.GetValue(columnIndex);
    }
}
