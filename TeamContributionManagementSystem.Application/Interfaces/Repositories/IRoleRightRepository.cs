using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IRoleRightRepository
{
    Task<List<RoleRight>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<RoleRight>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default);

    // Standardized naming
    Task<List<RoleRight>> GetAllRoleRightAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<RoleRight>> GetRoleRightAsyncByRole(UserRole role, CancellationToken cancellationToken = default) => GetByRoleAsync(role, cancellationToken);
}
