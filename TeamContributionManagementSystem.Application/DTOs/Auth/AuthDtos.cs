using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public DeviceDetailPayloadDto? DeviceInfo { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public IReadOnlyCollection<RoleRightDto> Rights { get; set; } = Array.Empty<RoleRightDto>();
    public bool RequiresTwoFactor { get; set; } = false;

    // Joined from Member profile
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoiningDate { get; set; }
    public string? Gender { get; set; }
    public string? MemberType { get; set; }
}

public class VerifyTwoFactorRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    public DeviceDetailPayloadDto DeviceInfo { get; set; }
}

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = CommonValidationMessages.PasswordMinLength)]
    public string NewPassword { get; set; } = string.Empty;
}
