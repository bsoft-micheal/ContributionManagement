using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IStatusRepository
{
    Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default);
    Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default);
    Task AddAsync(Status status, CancellationToken cancellationToken = default);
    void Update(Status status);
    void Delete(Status status);
}
