using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.DTOs.Auth;

public class LoginRequestDto
{
    private string _email = string.Empty;

    [Required(ErrorMessage = CommonValidationMessages.UsernameOrEmailRequired)]
    public string Email
    {
        get => _email;
        set => _email = value;
    }

    /// <summary>
    /// Optional alias for Email when username is sent explicitly in payload
    /// </summary>
    public string? Username
    {
        get => _email;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_email))
            {
                _email = value;
            }
        }
    }

    [Required(ErrorMessage = CommonValidationMessages.PasswordRequired)]
    public string Password { get; set; } = string.Empty;

    public bool IsFromMobile { get; set; } = false;
    public DeviceDetailPayloadDto? DeviceInfo { get; set; }
}

public class AuthResponseDto
{
    public Guid? UserId { get; set; }
    public Guid? MemberId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> PrimaryRoles { get; set; } = new();
    public List<Guid> PrimaryRoleIds { get; set; } = new();
    public List<string> SecondaryRoles { get; set; } = new();
    public List<Guid> SecondaryRoleIds { get; set; } = new();
    public bool EnableMultipleRoles { get; set; } = false;
    public bool IsPrimary { get; set; } = false;
    public bool IsSecondary { get; set; } = false;
    public Guid? ActiveRoleId { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public IReadOnlyCollection<RoleRightDto> Rights { get; set; } = Array.Empty<RoleRightDto>();
    public bool RequiresTwoFactor { get; set; } = false;
    public bool IsFirstLogin { get; set; } = false;

    // Joined from Member profile
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? Gender { get; set; }
    public string? WorkType { get; set; }
}

public class VerifyTwoFactorRequestDto
{
    [Required(ErrorMessage = CommonValidationMessages.UsernameOrEmailRequired)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = CommonValidationMessages.OtpRequired)]
    [StringLength(6, MinimumLength = 6, ErrorMessage = CommonValidationMessages.OtpExactLength)]
    public string Otp { get; set; } = string.Empty;

    public DeviceDetailPayloadDto? DeviceInfo { get; set; }
}

public class ForgotPasswordRequestDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = CommonValidationMessages.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;
}

public class SwitchRoleRequestDto
{
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public Guid? UserId { get; set; }
    public bool IsFromMobile { get; set; } = false;
    public DeviceDetailPayloadDto? DeviceInfo { get; set; }
}
