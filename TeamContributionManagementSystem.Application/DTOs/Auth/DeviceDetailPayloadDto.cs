namespace TeamContributionManagementSystem.Application.DTOs.Auth;

public class DeviceDetailPayloadDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public short DeviceType { get; set; } // 1 = Web Browser, 2 = Mobile App
    public string AppVersion { get; set; } = string.Empty;
    public long? TotalMemory { get; set; }
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
}
