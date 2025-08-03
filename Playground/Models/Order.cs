using System;
using System.Collections.Generic;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("orders")]
[Timestamps]
[SoftDelete]
public class Order : SmartModel<Order>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("order_number")]
    public string OrderNumber { get; set; } = string.Empty;
    
    [Column("user_id")]
    [ForeignKey(typeof(User), "Id")]
    public int UserId { get; set; }
    
    [Column("shipping_address_id")]
    [ForeignKey(typeof(Address), "Id")]
    public int? ShippingAddressId { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
    
    [Column("metadata", databaseType: "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    [Column("placed_at")]
    public DateTime? PlacedAt { get; set; }
    
    [Column("shipped_at")]
    public DateTime? ShippedAt { get; set; }
    
    [Column("delivered_at")]
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    [BelongsTo(typeof(User), "UserId")]
    public User? User { get; set; }
    
    [BelongsTo(typeof(Address), "ShippingAddressId")]
    public Address? ShippingAddress { get; set; }
    
    [HasMany(typeof(OrderItem), "OrderId")]
    public List<OrderItem> Items { get; set; } = new();
}