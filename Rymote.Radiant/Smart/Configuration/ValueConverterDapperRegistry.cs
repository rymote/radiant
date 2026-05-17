using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dapper;

namespace Rymote.Radiant.Smart.Configuration;

/// <summary>
/// Registers <see cref="ValueConverterTypeHandler{TClr}"/> instances with Dapper's
/// process-global <see cref="SqlMapper"/>. Dapper type handlers are AppDomain-wide; this registry
/// tracks which CLR types have already been registered to keep the registration idempotent across
/// repeated calls (multiple SmartContextOptions builds, multiple test fixtures, etc.).
/// </summary>
internal static class ValueConverterDapperRegistry
{
    private static readonly ConcurrentDictionary<Type, bool> registeredClrTypes = new();

    public static void Register(IReadOnlyDictionary<Type, ValueConverter> valueConverters)
    {
        foreach (KeyValuePair<Type, ValueConverter> entry in valueConverters)
        {
            Type clrType = entry.Key;
            ValueConverter converter = entry.Value;

            if (!registeredClrTypes.TryAdd(clrType, true))
                continue;

            Type handlerType = typeof(ValueConverterTypeHandler<>).MakeGenericType(clrType);
            object? handlerInstance = Activator.CreateInstance(handlerType, converter);
            if (handlerInstance is not SqlMapper.ITypeHandler dapperTypeHandler)
                throw new InvalidOperationException(
                    $"Failed to construct a Dapper type handler for CLR type '{clrType.FullName}'.");

            SqlMapper.AddTypeHandler(clrType, dapperTypeHandler);
        }
    }
}
