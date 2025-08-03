using System;
using System.Collections.Generic;
using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Playground.Models;

[Table("users")]
[Timestamps]
[SoftDelete]
public class User : SmartModel<User>
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Column("username")]
    public string Username { get; set; } = string.Empty;
    
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("profile_data", databaseType: "jsonb")]
    public string ProfileData { get; set; } = "{}";
    
    [Column("tags", databaseType: "text[]")]
    public string[]? Tags { get; set; }
    
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    [HasMany(typeof(Address), "UserId")]
    public List<Address> Addresses { get; set; } = new();
    
    [HasMany(typeof(Order), "UserId")]
    public List<Order> Orders { get; set; } = new();
}