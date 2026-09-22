namespace TeamContributionManagementSystem.Application.DTOs.Auth;

public class DeviceDetailPayloadDto
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public string Os { get; set; }
    public string OsVersion { get; set; }
    public string SystemName { get; set; }
    public string SystemVersion { get; set; }
    public short DeviceType { get; set; } // 1 = Web Browser, 2 = Mobile App
    public string AppVersion { get; set; }
    public long? TotalMemory { get; set; }
    public string Browser { get; set; }
    public string BrowserVersion { get; set; }
}
