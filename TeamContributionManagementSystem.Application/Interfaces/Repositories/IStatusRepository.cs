using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IStatusRepository
{
    Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, string? module, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetAllModuleAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default);
    Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(string statusName, CancellationToken cancellationToken = default);
    Task AddAsync(Status status, CancellationToken cancellationToken = default);
    void Update(Status status);
    void Delete(Status status);
}
