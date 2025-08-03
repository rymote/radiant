using System;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("addresses")]
[Timestamps]
[SoftDelete]
public class Address : SmartModel<Address>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("user_id")]
    [ForeignKey(typeof(User), "Id")]
    public int UserId { get; set; }
    
    [Column("type")]
    public string Type { get; set; } = string.Empty;
    
    [Column("street")]
    public string Street { get; set; } = string.Empty;
    
    [Column("city")]
    public string City { get; set; } = string.Empty;
    
    [Column("state")]
    public string? State { get; set; }
    
    [Column("country")]
    public string Country { get; set; } = string.Empty;
    
    [Column("postal_code")]
    public string? PostalCode { get; set; }
    
    [Column("is_primary")]
    public bool IsPrimary { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    [BelongsTo(typeof(User), "UserId")]
    public User? User { get; set; }
}