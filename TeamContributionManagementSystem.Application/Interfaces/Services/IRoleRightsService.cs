using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IRoleRightsService
{
    Task<IReadOnlyCollection<RoleRightDto>> GetAllRoleRightAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleRightDto>> GetRoleRightAsyncByRole(string roleName, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UpdateRoleRightsRequestDto request, string? user = null, CancellationToken cancellationToken = default);
}
