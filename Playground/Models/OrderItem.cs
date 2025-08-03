using System;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("order_items")]
[Timestamps]
public class OrderItem : SmartModel<OrderItem>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("order_id")]
    [ForeignKey(typeof(Order), "Id")]
    public int OrderId { get; set; }
    
    [Column("product_id")]
    [ForeignKey(typeof(Product), "Id")]
    public int ProductId { get; set; }
    
    [Column("quantity")]
    public int Quantity { get; set; } = 1;
    
    [Column("unit_price")]
    public decimal UnitPrice { get; set; }
    
    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }
    
    [Column("subtotal")]
    public decimal Subtotal { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    [BelongsTo(typeof(Order), "OrderId")]
    public Order? Order { get; set; }
    
    [BelongsTo(typeof(Product), "ProductId")]
    public Product? Product { get; set; }
}