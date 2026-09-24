using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> HasMembersAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Update(Role role);
    void Delete(Role role);

    // Standardized naming
    Task<List<Role>> GetAllRoleAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<Role?> GetRoleAsyncById(Guid roleId, CancellationToken cancellationToken = default) => GetByIdAsync(roleId, cancellationToken);
    Task SaveRoleAsync(Role role, CancellationToken cancellationToken = default) => AddAsync(role, cancellationToken);
    void UpdateRoleAsyncById(Role role) => Update(role);
    void DeleteRoleAsyncById(Role role) => Delete(role);
}
