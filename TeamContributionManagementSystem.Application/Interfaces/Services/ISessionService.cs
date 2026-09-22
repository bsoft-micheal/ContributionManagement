using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface ISessionService
{
    Task<IEnumerable<DeviceDetail>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeviceDetail>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task LogoutSessionAsync(Guid historyId, Guid userId, CancellationToken cancellationToken = default);
}
