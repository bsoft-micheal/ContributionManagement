using TeamContributionManagementSystem.Application.DTOs.Settings;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface ISystemSettingService
{
    Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> UpdateSettingsAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> ResetSettingsAsync(CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<SystemSettingsDto> GetSettingAsync(CancellationToken cancellationToken = default) => GetSettingsAsync(cancellationToken);
    Task<SystemSettingsDto> UpdateSettingAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default) => UpdateSettingsAsync(settings, user, cancellationToken);
    Task<SystemSettingsDto> ResetSettingAsync(CancellationToken cancellationToken = default) => ResetSettingsAsync(cancellationToken);
}
