using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Roles;

public class RoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal DefaultContributionAmount { get; set; }
}

public class CreateRoleRequestDto
{
    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal DefaultContributionAmount { get; set; }
}

public class UpdateRoleRequestDto : CreateRoleRequestDto
{
}
