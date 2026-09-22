using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IUserMfaDeviceRepository
{
    Task AddAsync(UserMfaDevice device, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserMfaDevice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RemoveAsync(UserMfaDevice device, CancellationToken cancellationToken = default);
    Task<UserMfaDevice?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
}
