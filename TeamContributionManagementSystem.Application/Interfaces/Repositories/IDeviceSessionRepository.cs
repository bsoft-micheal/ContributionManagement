using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IDeviceSessionRepository
{
    void Add(DeviceDetail deviceDetail);
    void Update(DeviceDetail deviceDetail);
    Task<DeviceDetail?> GetDeviceByDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceDetail>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceDetail>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<DeviceLoginHistory?> GetLoginHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);
    void AddLoginHistory(DeviceLoginHistory loginHistory);
}
