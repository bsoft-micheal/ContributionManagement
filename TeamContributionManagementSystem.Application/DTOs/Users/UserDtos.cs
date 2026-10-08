using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.DTOs.Auth;

namespace TeamContributionManagementSystem.Application.DTOs.Users;

public class UserDto
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool? CreateMemberProfile { get; set; }
    public bool? EnableUserAccess { get; set; }
    public Guid? RoleId { get; set; }
    public string? Role { get; set; }
    public string? RoleName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> PrimaryRoles { get; set; } = new();
    public List<string> SecondaryRoles { get; set; } = new();
    public string? SecondaryRole => SecondaryRoles.Count > 0 ? string.Join(", ", SecondaryRoles) : null;
    public string? SecondaryRolesCsv => SecondaryRoles.Count > 0 ? string.Join(", ", SecondaryRoles) : null;
    public List<Guid> RoleIds { get; set; } = new();
    public List<Guid> PrimaryRoleIds { get; set; } = new();
    public List<Guid> SecondaryRoleIds { get; set; } = new();
    public bool EnableMultipleRoles { get; set; } = false;
    public bool IsPrimary { get; set; } = false;
    public bool IsSecondary { get; set; } = false;
    public Guid? ActiveRoleId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; } = false;
    public bool IsFirstLogin { get; set; } = true;
    public bool HasMemberProfile { get; set; }
    public string? MemberUsername { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Joined from Member profile
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? WorkType { get; set; }
    public bool IsReferred { get; set; }
}

public class CreateUserRequestDto
{
    public Guid? MemberId { get; set; }

    [MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? RoleName { get; set; }

    public List<string>? Roles { get; set; }
    public string? RolesCsv { get; set; }
    public List<Guid>? RoleIds { get; set; }
    public List<string>? PrimaryRoles { get; set; }
    public List<string>? SecondaryRoles { get; set; }
    public string? SecondaryRole { get; set; }
    public string? SecondaryRolesCsv { get; set; }
    public List<Guid>? PrimaryRoleIds { get; set; }
    public List<Guid>? SecondaryRoleIds { get; set; }
    public bool? EnableMultipleRoles { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsSecondary { get; set; }
    public Guid? ActiveRoleId { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? WorkType { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }

    public bool? CreateMemberProfile { get; set; }
    public bool? EnableUserAccess { get; set; }

    [MaxLength(100)]
    public string? MemberUsername { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequestDto
{
    [MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(100)]
    public string? Username { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional – only set when the caller wants to change the password.</summary>
    [MaxLength(64)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? RoleName { get; set; }

    public List<string>? Roles { get; set; }
    public string? RolesCsv { get; set; }
    public List<Guid>? RoleIds { get; set; }
    public List<string>? PrimaryRoles { get; set; }
    public List<string>? SecondaryRoles { get; set; }
    public string? SecondaryRole { get; set; }
    public string? SecondaryRolesCsv { get; set; }
    public List<Guid>? PrimaryRoleIds { get; set; }
    public List<Guid>? SecondaryRoleIds { get; set; }
    public bool? EnableMultipleRoles { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsSecondary { get; set; }
    public Guid? ActiveRoleId { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? WorkType { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }

    public bool? CreateMemberProfile { get; set; }
    public bool? EnableUserAccess { get; set; }

    [MaxLength(100)]
    public string? MemberUsername { get; set; }

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

    public string? Password { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }

    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string WorkType { get; set; } = string.Empty;
}

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "New password must be exactly 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
