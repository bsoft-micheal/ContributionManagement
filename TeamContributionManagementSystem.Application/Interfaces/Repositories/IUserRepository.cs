using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetFirstAdminAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
    void Update(AppUser user);
    void Delete(AppUser user);

    // Standardized naming
    Task<List<UserDto>> GetAllUserAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<AppUser?> GetUserAsyncById(Guid userId, CancellationToken cancellationToken = default) => GetByIdAsync(userId, cancellationToken);
    Task SaveUserAsync(AppUser user, CancellationToken cancellationToken = default) => AddAsync(user, cancellationToken);
    void UpdateUserAsyncById(AppUser user) => Update(user);
    void DeleteUserAsyncById(AppUser user) => Delete(user);
}
