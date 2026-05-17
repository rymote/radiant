using System;

namespace Rymote.Radiant.Smart.Configuration;

public abstract class ValueConverter
{
    public abstract Type ClrType { get; }
    public abstract Type DatabaseType { get; }
    public abstract object? ToDatabase(object? clrValue);
    public abstract object? FromDatabase(object? databaseValue);
}

public sealed class ValueConverter<TClr, TDatabase> : ValueConverter
{
    private readonly Func<TClr, TDatabase> toDatabaseConverter;
    private readonly Func<TDatabase, TClr> fromDatabaseConverter;

    public ValueConverter(Func<TClr, TDatabase> toDatabase, Func<TDatabase, TClr> fromDatabase)
    {
        toDatabaseConverter = toDatabase;
        fromDatabaseConverter = fromDatabase;
    }

    public override Type ClrType => typeof(TClr);
    public override Type DatabaseType => typeof(TDatabase);

    public override object? ToDatabase(object? clrValue)
        => clrValue is null ? null : toDatabaseConverter((TClr)clrValue);

    public override object? FromDatabase(object? databaseValue)
        => databaseValue is null ? null : fromDatabaseConverter((TDatabase)databaseValue);
}
