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

/// <summary>
/// Response DTO for MFA setup generation.
/// </summary>
public class MfaSetupResponseDto
{
    public string SecretKey { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
}

