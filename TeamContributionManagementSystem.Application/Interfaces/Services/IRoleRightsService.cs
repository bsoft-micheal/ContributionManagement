using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IRoleRightsService
{
    Task<IReadOnlyCollection<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleRightDto>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UpdateRoleRightsRequestDto request, string? user = null, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<RoleRightDto>> GetAllRoleRightAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<IReadOnlyCollection<RoleRightDto>> GetRoleRightAsyncByRole(string roleName, CancellationToken cancellationToken = default) => GetByRoleAsync(roleName, cancellationToken);
}
