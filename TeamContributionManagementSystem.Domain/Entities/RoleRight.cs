using System.ComponentModel.DataAnnotations.Schema;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class RoleRight
{
    public Guid RoleRightId { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public int FeatureID { get; set; }
    public NavigationMenu? NavigationMenu { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Module { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string SubModule { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Page { get; set; } = string.Empty;

    public string Access { get; set; } = "readWrite"; // "readOnly", "readWrite", "deny"
    public AccessType AccessType { get; set; } = AccessType.ReadWrite;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
