using System;
using System.Collections.Generic;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("categories")]
[Timestamps]
[SoftDelete]
public class Category : SmartModel<Category>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("parent_id")]
    public int? ParentId { get; set; }
    
    [Column("metadata", databaseType: "jsonb")]
    public string Metadata { get; set; } = "{}";
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    [BelongsTo(typeof(Category), "ParentId")]
    public Category? Parent { get; set; }
    
    [HasMany(typeof(Category), "ParentId")]
    public List<Category> Children { get; set; } = new();
}