using System.ComponentModel.DataAnnotations;

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

    [Required]
    [MaxLength(100)]
    public string SubModule { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Page { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Access { get; set; } = string.Empty; // "readOnly", "readWrite", "deny"

    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}


public class UpdateRoleRightsRequestDto
{
    [Required]
    [MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    public List<RoleRightDto> Rights { get; set; } = new();
}
