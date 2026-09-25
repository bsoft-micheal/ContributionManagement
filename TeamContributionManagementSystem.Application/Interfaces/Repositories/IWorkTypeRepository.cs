using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IWorkTypeRepository
{
    Task<IReadOnlyCollection<WorkType>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<WorkType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(WorkType workType, CancellationToken cancellationToken = default);
    void Update(WorkType workType);
    void Delete(WorkType workType);
}
