using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface ISystemSettingRepository
{
    Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SystemSetting> settings, CancellationToken cancellationToken = default);
    void Update(SystemSetting setting);

    // Standardized naming
    Task<List<SystemSetting>> GetAllSettingAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<SystemSetting?> GetSettingAsyncByKey(string key, CancellationToken cancellationToken = default) => GetByKeyAsync(key, cancellationToken);
    void UpdateSettingAsync(SystemSetting setting) => Update(setting);
}
