using System;
using System.Collections.Generic;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("products")]
[Timestamps]
[SoftDelete]
public class Product : SmartModel<Product>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("sku")]
    public string Sku { get; set; } = string.Empty;
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("price")]
    public decimal Price { get; set; }
    
    [Column("stock_quantity")]
    public int StockQuantity { get; set; }
    
    [Column("attributes", databaseType: "jsonb")]
    public string Attributes { get; set; } = "{}";
    
    [Column("tags")]
    public string[]? Tags { get; set; }
    
    [Column("search_vector")]
    public string? SearchVector { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}