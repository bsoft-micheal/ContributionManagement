using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.DTOs.Users;

public class RoleRightDto
{
    public Guid RoleRightId { get; set; }
    public int FeatureID { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SubModule { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Page { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Access { get; set; } = string.Empty; // "readOnly", "readWrite", "deny"

    public int AccessType { get; set; } = 2; // 1 = ReadOnly, 2 = ReadWrite, 3 = Deny

    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
}


public class UpdateRoleRightsRequestDto
{
    [Required]
    [MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    public List<RoleRightDto> Rights { get; set; } = new();
}
