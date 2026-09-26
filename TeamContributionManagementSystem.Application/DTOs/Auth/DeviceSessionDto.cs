namespace TeamContributionManagementSystem.Application.DTOs.Auth;

public class DeviceSessionDto
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public short DeviceType { get; set; }
    public string AppVersion { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LogoutTime { get; set; }
}
