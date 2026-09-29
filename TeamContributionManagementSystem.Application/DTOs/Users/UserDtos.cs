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

    public string? Password { get; set; }

    [MaxLength(50)]
    public string? RoleName { get; set; }

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
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? RoleName { get; set; }

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
