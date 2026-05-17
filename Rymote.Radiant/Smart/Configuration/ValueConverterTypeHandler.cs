using System;
using System.Data;
using Dapper;

namespace Rymote.Radiant.Smart.Configuration;

/// <summary>
/// Bridges a Radiant <see cref="ValueConverter"/> into Dapper's
/// <see cref="SqlMapper.TypeHandler{T}"/> contract so result-mapped CLR properties of type
/// <typeparamref name="TClr"/> are converted automatically from their database representation.
/// </summary>
internal sealed class ValueConverterTypeHandler<TClr> : SqlMapper.TypeHandler<TClr>
{
    private readonly ValueConverter underlyingConverter;

    public ValueConverterTypeHandler(ValueConverter underlyingConverter)
    {
        this.underlyingConverter = underlyingConverter ?? throw new ArgumentNullException(nameof(underlyingConverter));
    }

    public override void SetValue(IDbDataParameter parameter, TClr? value)
    {
        parameter.Value = underlyingConverter.ToDatabase(value) ?? DBNull.Value;
    }

    public override TClr Parse(object value)
    {
        object? clrValue = underlyingConverter.FromDatabase(value);
        return clrValue is null ? default! : (TClr)clrValue;
    }
}
