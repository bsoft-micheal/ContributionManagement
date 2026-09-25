using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class RoleRight
{
    public Guid RoleRightId { get; set; }
    public UserRole Role { get; set; }
    public int FeatureID { get; set; }
    public NavigationMenu? NavigationMenu { get; set; }
    public string Module { get; set; } = string.Empty;
    public string SubModule { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty; // "readOnly", "readWrite", "deny"
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
