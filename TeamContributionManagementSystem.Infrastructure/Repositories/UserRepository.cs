using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .OrderBy(x => x.Username)
                .Select(x => new UserDto
                {
                    UserId = x.UserId,
                    Username = x.Username,
                    FullName = x.FullName,
                    Email = x.Email,
                    RoleName = x.UserRoles.Where(ur => ur.IsActive && !ur.IsDeleted).Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Member",
                    IsActive = x.IsActive,
                    IsFirstLogin = x.IsFirstLogin,
                    ProfileImage = x.ProfileImage,
                    CreatedOn = x.CreatedOn,
                    CreatedAt = x.CreatedAt,
                    CreatedBy = x.CreatedBy
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.MfaDevices)
                .Include(u => u.UserRoles.Where(ur => ur.IsActive && !ur.IsDeleted))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEmailAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.MfaDevices)
                .Include(u => u.UserRoles.Where(ur => ur.IsActive && !ur.IsDeleted))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByUsernameAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.MfaDevices)
                .Include(u => u.UserRoles.Where(ur => ur.IsActive && !ur.IsDeleted))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetFirstAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.UserRoles.Any(ur => ur.Role.RoleName.ToLower() == "admin" && ur.IsActive && !ur.IsDeleted) && x.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetFirstAdminAsync));
            throw;
        }
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Users.AddAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(AppUser user)
    {
        try
        {
            _context.Users.Update(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(AppUser user)
    {
        try
        {
            _context.Users.Remove(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }

    public async Task CascadeUpdateCreatorDisplayNameAsync(Guid userId, string oldName, string newName, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdStr = userId.ToString();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            var username = user?.Username ?? string.Empty;

            var oldLower = (oldName ?? string.Empty).Trim().ToLowerInvariant();
            var userLower = username.Trim().ToLowerInvariant();

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE gallery_photos 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE gallery_photos 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE expenses 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE expenses 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE members 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE members 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE support_tickets 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE support_tickets 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE priorities 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE priorities 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE statuses 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE statuses 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ticket_types 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ticket_types 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE work_types 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE work_types 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE budget_calculations 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE budget_calculations 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE event_types 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(event_types.created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE event_types 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(event_types.modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE roles 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(roles.created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE roles 
                SET modified_by = {newName} 
                WHERE modified_by = {userIdStr} 
                   OR LOWER(modified_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(roles.modified_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE contributions 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(contributions.created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE payment_transactions 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(payment_transactions.created_by) = {userLower})", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cascade update creator display name");
        }
    }
}
