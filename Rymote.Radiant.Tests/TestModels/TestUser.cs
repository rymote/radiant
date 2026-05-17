using Rymote.Radiant.Smart;
using Rymote.Radiant.Smart.Attributes;

namespace Rymote.Radiant.Tests.TestModels;

[Table("users")]
public class TestUser : SmartModel<TestUser>
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("age")]
    public int Age { get; set; }
}
