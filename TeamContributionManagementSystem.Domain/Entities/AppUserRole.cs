using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("user_roles")]
public class AppUserRole
{
    [NotMapped]
    public Guid UserRoleId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    [NotMapped]
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public bool IsDeleted { get; set; } = false;

    [NotMapped]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public Guid? ModifiedBy { get; set; }

    [NotMapped]
    public DateTime? ModifiedOn { get; set; }
}
