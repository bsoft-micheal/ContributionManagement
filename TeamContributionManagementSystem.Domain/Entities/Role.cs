using TeamContributionManagementSystem.Domain.Common;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Role : IAuditableEntity
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal DefaultContributionAmount { get; set; }

    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<RoleRight> RoleRights { get; set; } = new List<RoleRight>();
    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
