using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Users;

public class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateUserRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional – only set when the caller wants to change the password.</summary>
    [MinLength(6)]
    public string? Password { get; set; }

    [Required]
    [MaxLength(20)]
    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateProfileRequestDto
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    public string? ProfileImage { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }
}
