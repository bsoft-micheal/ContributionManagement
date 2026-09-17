using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Users;

public class RoleRightDto
{
    [Required]
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubModule { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Page { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Access { get; set; } = string.Empty; // "readOnly", "readWrite", "deny"
}

public class UpdateRoleRightsRequestDto
{
    [Required]
    [MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    public List<RoleRightDto> Rights { get; set; } = new();
}
