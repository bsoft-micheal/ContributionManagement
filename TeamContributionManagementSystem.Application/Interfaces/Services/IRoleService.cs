using TeamContributionManagementSystem.Application.DTOs.Roles;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<RoleDto>> GetAllRoleAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<RoleDto> SaveRoleAsync(CreateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<RoleDto> UpdateRoleAsyncById(Guid roleId, UpdateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(roleId, request, user, cancellationToken);
    Task DeleteRoleAsyncById(Guid roleId, CancellationToken cancellationToken = default) => DeleteAsync(roleId, cancellationToken);
}
