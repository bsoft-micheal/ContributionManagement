using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IStatusRepository
{
    Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Status status, CancellationToken cancellationToken = default);
    void Update(Status status);
    void Delete(Status status);
}
