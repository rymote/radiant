namespace Rymote.Radiant.Tests.TestModels;

/// <summary>
/// A strongly-typed identifier value object — the canonical case for a registered
/// <c>ValueConverter&lt;ContactId, string&gt;</c>.
/// </summary>
public readonly record struct ContactId(string Value)
{
    public override string ToString() => Value;
}
