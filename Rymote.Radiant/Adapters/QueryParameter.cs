using System.Data;

namespace Rymote.Radiant.Adapters;

public sealed record QueryParameter(
    string Name,
    object? Value,
    DbType? Type = null,
    string? DatabaseNativeType = null);
