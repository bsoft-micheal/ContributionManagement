using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("user_roles")]
public class AppUserRole
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
