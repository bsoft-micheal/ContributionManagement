using TeamContributionManagementSystem.Application.DTOs.Roles;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<RoleDto>> GetAllRoleAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<RoleDto> SaveRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<RoleDto> UpdateRoleAsyncById(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(roleId, request, cancellationToken);
    Task DeleteRoleAsyncById(Guid roleId, CancellationToken cancellationToken = default) => DeleteAsync(roleId, cancellationToken);
}
