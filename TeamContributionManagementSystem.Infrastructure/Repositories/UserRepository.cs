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

    public async Task<HashSet<string>> GetReferencedUserIdentifiersAsync(CancellationToken cancellationToken = default)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var contributionMemberIds = await _context.Contributions
                .Where(c => !c.IsDeleted)
                .Select(c => c.UserId.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in contributionMemberIds) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Contributions sub-query"); }

        try
        {
            var participantMemberIds = await _context.EventParticipants
                .Select(ep => ep.UserId.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in participantMemberIds) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping EventParticipants sub-query"); }

        try
        {
            var eventCreators = await _context.Events
                .Where(e => !e.IsDeleted && e.CreatedBy != Guid.Empty)
                .Select(e => e.CreatedBy.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in eventCreators) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Events sub-query"); }

        try
        {
            var paymentTxnUserIds = await _context.PaymentTransactions
                .Where(pt => !pt.IsDeleted && pt.UserId.HasValue)
                .Select(pt => pt.UserId!.Value.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in paymentTxnUserIds) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping PaymentTransactions.UserId sub-query"); }

        try
        {
            var expenseUsers = await _context.Expenses
                .Where(ex => !ex.IsDeleted && !string.IsNullOrEmpty(ex.SubmittedBy))
                .Select(ex => ex.SubmittedBy.Trim().ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var name in expenseUsers) set.Add(name);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Expenses sub-query"); }

        try
        {
            var ticketMembers = await _context.SupportTickets
                .Where(st => !st.IsDeleted && st.User != null && !string.IsNullOrEmpty(st.User.FullName))
                .Select(st => st.User!.FullName.Trim().ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var name in ticketMembers) set.Add(name);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping SupportTickets.MemberName sub-query"); }

        try
        {
            var ticketMemberIds = await _context.SupportTickets
                .Where(st => !st.IsDeleted && st.UserId != null)
                .Select(st => (st.UserId.HasValue ? st.UserId.Value.ToString() : string.Empty).ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in ticketMemberIds) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping SupportTickets.MemberId sub-query"); }

        try
        {
            var ticketAssigned = await _context.SupportTickets
                .Where(st => !st.IsDeleted && !string.IsNullOrEmpty(st.AssignedTo))
                .Select(st => st.AssignedTo!.Trim().ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var name in ticketAssigned) set.Add(name);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping SupportTickets.AssignedTo sub-query"); }

        return set;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var referenced = await GetReferencedUserIdentifiersAsync(cancellationToken);

            var users = await _context.Users
                .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .OrderBy(x => x.FullName)
                .Select(x => new UserDto
                {
                    UserId = x.UserId,
                    Username = x.Username,
                    FullName = x.FullName,
                    Email = x.Email,
                    RoleName = x.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member",
                    IsActive = x.IsActive,
                    IsDeleted = x.IsDeleted,
                    IsFirstLogin = x.IsFirstLogin,
                    ProfileImage = x.ProfileImage,
                    CreatedOn = x.CreatedOn,
                    CreatedAt = x.CreatedAt,
                    CreatedBy = x.CreatedBy,
                    HasMemberProfile = true,
                    MemberUsername = x.FullName,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    Phone = x.Phone,
                    WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty
                })
                .ToListAsync(cancellationToken);

            foreach (var u in users)
            {
                var uid = u.UserId.ToString();
                var uname = u.Username.Trim().ToLowerInvariant();
                var fname = u.FullName.Trim().ToLowerInvariant();
                var email = u.Email.Trim().ToLowerInvariant();

                u.IsReferred = referenced.Contains(uid) || referenced.Contains(uname) || referenced.Contains(fname) || referenced.Contains(email);
            }

            return users;
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.IsActive && x.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName.ToLower() == "admin"), cancellationToken);
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
                UPDATE users 
                SET created_by = {newName} 
                WHERE created_by = {userIdStr} 
                   OR LOWER(created_by) = {oldLower} 
                   OR ({userLower} <> '' AND LOWER(created_by) = {userLower})", cancellationToken);

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE users 
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
