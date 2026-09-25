using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<UserDto>> GetAllUserAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<UserDto> SaveUserAsync(CreateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<UserDto> UpdateUserAsyncById(Guid userId, UpdateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(userId, request, user, cancellationToken);
    Task DeleteUserAsyncById(Guid userId, CancellationToken cancellationToken = default) => DeleteAsync(userId, cancellationToken);
}
