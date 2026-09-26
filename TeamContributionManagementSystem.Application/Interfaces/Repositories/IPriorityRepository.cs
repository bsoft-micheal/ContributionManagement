using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IPriorityRepository
{
    Task<IReadOnlyCollection<Priority>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<Priority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Priority?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(Priority priority, CancellationToken cancellationToken = default);
    void Update(Priority priority);
    void Delete(Priority priority);
}
