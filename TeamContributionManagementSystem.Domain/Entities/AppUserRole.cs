using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("user_roles")]
public class AppUserRole
{
    public Guid UserRoleId { get; set; }
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
