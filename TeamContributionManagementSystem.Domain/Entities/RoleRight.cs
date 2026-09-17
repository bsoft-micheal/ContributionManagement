using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class RoleRight
{
    public Guid RoleRightId { get; set; }
    public UserRole Role { get; set; }
    public string Module { get; set; } = string.Empty;
    public string SubModule { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty; // "readOnly", "readWrite", "deny"
}
