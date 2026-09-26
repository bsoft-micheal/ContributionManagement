namespace TeamContributionManagementSystem.Application.DTOs.Mfa;

/// <summary>
/// Response DTO for user MFA device information.
/// </summary>
public class UserMfaDeviceDto
{
    public Guid Id { get; set; }
    public string DeviceLabel { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
}
