using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Rymote.Radiant.Tests.TestModels;

/// <summary>
/// Exercises the source-generated result-mapper round-trip for a model whose primary key uses a
/// strongly-typed value object (<see cref="ContactId"/>). When a <c>ValueConverter</c> is registered
/// for <see cref="ContactId"/>, the mapper must apply it at read time rather than calling
/// <c>DbDataReader.GetFieldValue&lt;ContactId&gt;</c> directly (which the database provider has no
/// way of resolving).
/// </summary>
[Table("orders_with_strong_id")]
public class OrderWithStronglyTypedId : SmartModel<OrderWithStronglyTypedId>
{
    [PrimaryKey(isAutoIncrement: false)]
    [Column("id")]
    public ContactId Id { get; set; }

    [Column("status")]
    public string Status { get; set; } = string.Empty;
}
