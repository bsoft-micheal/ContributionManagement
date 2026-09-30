using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IRoleRightRepository
{
    Task<List<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<RoleRightDto>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<List<RoleRightDto>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<List<RoleRightDto>> GetByRoleNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(Guid roleId, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default);

    // Standardized naming
    Task<List<RoleRightDto>> GetAllRoleRightAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<RoleRightDto>> GetRoleRightAsyncByRole(UserRole role, CancellationToken cancellationToken = default) => GetByRoleAsync(role, cancellationToken);
    Task<List<RoleRightDto>> GetRoleRightAsyncByRole(string roleName, CancellationToken cancellationToken = default) => GetByRoleNameAsync(roleName, cancellationToken);
}
