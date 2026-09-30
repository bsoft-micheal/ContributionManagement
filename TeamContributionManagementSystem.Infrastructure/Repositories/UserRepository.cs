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

            var userEntities = await _context.Users
                .AsNoTracking()
                .Include(x => x.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(x => x.WorkTypeNavigation)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);

            var users = userEntities
                .Select(x =>
                {
                    var assignedRoles = x.UserRoles?.Where(ur => ur.Role != null).ToList() ?? new List<AppUserRole>();
                    var activeRole = (x.RoleId.HasValue ? assignedRoles.FirstOrDefault(ur => ur.RoleId == x.RoleId.Value)?.Role?.RoleName : null)
                        ?? assignedRoles.FirstOrDefault(ur => ur.IsPrimary)?.Role?.RoleName
                        ?? assignedRoles.FirstOrDefault()?.Role?.RoleName
                        ?? "Member";

                    var allRoleNames = assignedRoles.Select(ur => ur.Role!.RoleName).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var primaryRoleNames = assignedRoles.Where(ur => ur.IsPrimary).Select(ur => ur.Role!.RoleName).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var secondaryRoleNames = assignedRoles.Where(ur => ur.IsSecondary).Select(ur => ur.Role!.RoleName).Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    return new UserDto
                    {
                        UserId = x.UserId,
                        Username = x.Username,
                        FullName = x.FullName,
                        Email = x.Email,
                        RoleName = activeRole,
                        Roles = allRoleNames,
                        RoleIds = assignedRoles.Select(ur => ur.RoleId).Distinct().ToList(),
                        PrimaryRoles = primaryRoleNames,
                        SecondaryRoles = secondaryRoleNames,
                        EnableMultipleRoles = x.EnableMultipleRoles,
                        IsPrimary = x.IsPrimary || assignedRoles.Any(ur => ur.IsPrimary),
                        IsSecondary = x.IsSecondary || assignedRoles.Any(ur => ur.IsSecondary),
                        ActiveRoleId = x.RoleId,
                        IsActive = x.IsActive,
                        IsDeleted = x.IsDeleted,
                        IsFirstLogin = x.IsFirstLogin,
                        ProfileImage = x.ProfileImage,
                        CreatedOn = x.CreatedOn,
                        CreatedAt = x.CreatedAt,
                        CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                        HasMemberProfile = true,
                        MemberUsername = x.FullName,
                        DateOfBirth = x.DateOfBirth,
                        JoiningDate = x.JoiningDate,
                        Gender = x.Gender,
                        Phone = x.Phone,
                        WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty
                    };
                })
                .ToList();

            var userDict = users.ToDictionary(u => u.UserId, u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username);

            foreach (var u in users)
            {
                var uid = u.UserId.ToString();
                var uname = u.Username.Trim().ToLowerInvariant();
                var fname = u.FullName.Trim().ToLowerInvariant();
                var email = u.Email.Trim().ToLowerInvariant();

                u.IsReferred = referenced.Contains(uid) || referenced.Contains(uname) || referenced.Contains(fname) || referenced.Contains(email);

                if (u.Roles.Count == 0 && !string.IsNullOrWhiteSpace(u.RoleName))
                {
                    u.Roles.Add(u.RoleName);
                }
                if (u.PrimaryRoles.Count == 0 && u.Roles.Count > 0)
                {
                    u.PrimaryRoles.Add(u.Roles.First());
                    u.IsPrimary = true;
                }

                if (!string.IsNullOrWhiteSpace(u.CreatedBy) && Guid.TryParse(u.CreatedBy, out var cGuid) && userDict.TryGetValue(cGuid, out var cName))
                {
                    u.CreatedBy = cName;
                }
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

    public Task CascadeUpdateCreatorDisplayNameAsync(Guid userId, string oldName, string newName, CancellationToken cancellationToken = default)
    {
        // No-op: Audit columns created_by and modified_by in all tables store UUID foreign keys to users.user_id.
        // User display names are resolved dynamically at query time from the users table.
        return Task.CompletedTask;
    }
}
