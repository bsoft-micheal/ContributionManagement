namespace TeamContributionManagementSystem.Application.DTOs.Mfa;

/// <summary>
/// Request DTO for verifying TOTP setup.
/// </summary>
public class MfaSetupVerifyRequestDto
{
    public string SecretKey { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}
