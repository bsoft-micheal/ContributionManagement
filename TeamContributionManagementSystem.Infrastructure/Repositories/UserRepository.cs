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

        // 1. Contributions
        try
        {
            var contributions = await _context.Contributions
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.UserId, c.CreatedBy })
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var c in contributions)
            {
                if (c.UserId != Guid.Empty) set.Add(c.UserId.ToString());
                if (c.CreatedBy.HasValue && c.CreatedBy.Value != Guid.Empty) set.Add(c.CreatedBy.Value.ToString());
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Contributions sub-query"); }

        // 2. Event Participants
        try
        {
            var participants = await _context.EventParticipants
                .AsNoTracking()
                .Where(ep => !ep.IsDeleted)
                .Select(ep => new { ep.UserId, ep.CreatedBy })
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var ep in participants)
            {
                if (ep.UserId != Guid.Empty) set.Add(ep.UserId.ToString());
                if (ep.CreatedBy.HasValue && ep.CreatedBy.Value != Guid.Empty) set.Add(ep.CreatedBy.Value.ToString());
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping EventParticipants sub-query"); }

        // 3. Events Created By
        try
        {
            var eventCreators = await _context.Events
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.CreatedBy != Guid.Empty)
                .Select(e => e.CreatedBy.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in eventCreators) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Events sub-query"); }

        // 4. Payment Transactions
        try
        {
            var paymentTxns = await _context.PaymentTransactions
                .AsNoTracking()
                .Where(pt => !pt.IsDeleted)
                .Select(pt => new { pt.UserId, pt.CreatedBy, pt.VerifiedBy })
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var pt in paymentTxns)
            {
                if (pt.UserId.HasValue && pt.UserId.Value != Guid.Empty) set.Add(pt.UserId.Value.ToString());
                if (pt.CreatedBy.HasValue && pt.CreatedBy.Value != Guid.Empty) set.Add(pt.CreatedBy.Value.ToString());
                if (!string.IsNullOrWhiteSpace(pt.VerifiedBy)) set.Add(pt.VerifiedBy.Trim().ToLowerInvariant());
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping PaymentTransactions sub-query"); }

        // 5. Expenses
        try
        {
            var expenses = await _context.Expenses
                .AsNoTracking()
                .Where(ex => !ex.IsDeleted)
                .Select(ex => new { ex.SubmittedBy, ex.ApprovedBy, ex.CreatedBy })
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var ex in expenses)
            {
                if (!string.IsNullOrWhiteSpace(ex.SubmittedBy)) set.Add(ex.SubmittedBy.Trim().ToLowerInvariant());
                if (!string.IsNullOrWhiteSpace(ex.ApprovedBy)) set.Add(ex.ApprovedBy.Trim().ToLowerInvariant());
                if (ex.CreatedBy.HasValue && ex.CreatedBy.Value != Guid.Empty) set.Add(ex.CreatedBy.Value.ToString());
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Expenses sub-query"); }

        // 6. Support Tickets
        try
        {
            var tickets = await _context.SupportTickets
                .AsNoTracking()
                .Where(st => !st.IsDeleted)
                .Select(st => new { st.UserId, st.AssignedTo, st.CreatedBy })
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var st in tickets)
            {
                if (st.UserId.HasValue && st.UserId.Value != Guid.Empty) set.Add(st.UserId.Value.ToString());
                if (!string.IsNullOrWhiteSpace(st.AssignedTo)) set.Add(st.AssignedTo.Trim().ToLowerInvariant());
                if (st.CreatedBy.HasValue && st.CreatedBy.Value != Guid.Empty) set.Add(st.CreatedBy.Value.ToString());
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping SupportTickets sub-query"); }

        // 7. Gallery Photos
        try
        {
            var galleryPhotos = await _context.GalleryPhotos
                .AsNoTracking()
                .Where(gp => !gp.IsDeleted && gp.CreatedBy.HasValue && gp.CreatedBy.Value != Guid.Empty)
                .Select(gp => gp.CreatedBy!.Value.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in galleryPhotos) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping GalleryPhotos sub-query"); }

        // 8. Budget Calculations
        try
        {
            var budgetCalculations = await _context.BudgetCalculations
                .AsNoTracking()
                .Where(bc => !bc.IsDeleted && bc.CreatedBy.HasValue && bc.CreatedBy.Value != Guid.Empty)
                .Select(bc => bc.CreatedBy!.Value.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in budgetCalculations) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping BudgetCalculations sub-query"); }

        // 9. Users Created By
        try
        {
            var usersCreatedBy = await _context.Users
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.CreatedBy.HasValue && u.CreatedBy.Value != Guid.Empty)
                .Select(u => u.CreatedBy!.Value.ToString())
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var id in usersCreatedBy) set.Add(id);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GetReferencedUserIdentifiers: skipping Users Created By sub-query"); }

        return set;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var referenced = await GetReferencedUserIdentifiersAsync(cancellationToken);

            var userEntities = await _context.Users
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(x => x.WorkTypeNavigation)
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);

            var users = userEntities
                .Select(x =>
                {
                    var assignedRoles = x.UserRoles?.Where(ur => ur.Role != null && !ur.Role.IsDeleted).ToList() ?? new List<AppUserRole>();
                    var primaryRoleNames = assignedRoles.Where(ur => ur.IsPrimary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var secondaryRoleNames = assignedRoles.Where(ur => ur.IsSecondary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var allRoleNames = assignedRoles.Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    var primaryRoleIds = assignedRoles.Where(ur => ur.IsPrimary).Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();
                    var secondaryRoleIds = assignedRoles.Where(ur => ur.IsSecondary).Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();
                    var allRoleIds = assignedRoles.Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();

                    Guid? activeRoleId = (x.RoleId.HasValue && x.RoleId.Value != Guid.Empty)
                        ? x.RoleId
                        : (primaryRoleIds.Count > 0
                            ? primaryRoleIds.First()
                            : (allRoleIds.Count > 0 ? allRoleIds.First() : (Guid?)null));

                    var primaryRoleName = primaryRoleNames.FirstOrDefault() ?? assignedRoles.FirstOrDefault()?.Role?.RoleName;

                    return new UserDto
                    {
                        UserId = x.UserId,
                        Username = x.Username,
                        FullName = x.FullName,
                        Email = x.Email,
                        RoleId = (activeRoleId.HasValue && activeRoleId.Value != Guid.Empty) ? activeRoleId : null,
                        Role = primaryRoleName,
                        RoleName = primaryRoleName,
                        Roles = allRoleNames,
                        PrimaryRoles = primaryRoleNames,
                        SecondaryRoles = secondaryRoleNames,
                        RoleIds = allRoleIds,
                        PrimaryRoleIds = primaryRoleIds,
                        SecondaryRoleIds = secondaryRoleIds,
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
                var matchingEntity = userEntities.FirstOrDefault(e => e.UserId == u.UserId);
                var hasLoginAccount = !string.IsNullOrWhiteSpace(u.Username) || !string.IsNullOrWhiteSpace(matchingEntity?.PasswordHash);

                var uid = u.UserId.ToString();
                var uname = !string.IsNullOrWhiteSpace(u.Username) ? u.Username.Trim().ToLowerInvariant() : string.Empty;
                var fname = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName.Trim().ToLowerInvariant() : string.Empty;
                var email = !string.IsNullOrWhiteSpace(u.Email) ? u.Email.Trim().ToLowerInvariant() : string.Empty;

                var isReferencedInDb = (!string.IsNullOrEmpty(uid) && referenced.Contains(uid))
                    || (!string.IsNullOrEmpty(uname) && referenced.Contains(uname))
                    || (!string.IsNullOrEmpty(fname) && referenced.Contains(fname))
                    || (!string.IsNullOrEmpty(email) && referenced.Contains(email));

                u.IsReferred = hasLoginAccount || isReferencedInDb;

                if (u.RoleIds.Count == 0 && u.RoleId.HasValue)
                {
                    u.RoleIds.Add(u.RoleId.Value);
                }
                if (u.PrimaryRoleIds.Count == 0 && u.RoleIds.Count > 0)
                {
                    u.PrimaryRoleIds.Add(u.RoleIds.First());
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
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Email.ToLower() == email.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEmailAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default)
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEmailIncludingDeletedAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            var lowerUsername = username.Trim().ToLower();
            return await _context.Users
                .Include(u => u.MfaDevices)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Username != null && x.Username.ToLower() == lowerUsername, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByUsernameAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanDigits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(cleanDigits)) return null;

            return await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Phone != null && x.Phone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanDigits, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByPhoneAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetByUsernameOrEmailAsync(string identifier, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var trimmed = identifier.Trim();
            var normalizedEmail = trimmed.ToLower();
            return await _context.Users
                .Include(u => u.MfaDevices)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.WorkTypeNavigation)
                .FirstOrDefaultAsync(x => !x.IsDeleted && (x.Email.ToLower() == normalizedEmail || (x.Username != null && x.Username.ToLower() == normalizedEmail)), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByUsernameOrEmailAsync));
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
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.UserId == userId, cancellationToken);
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
            var entry = _context.Entry(user);
            if (entry.State == EntityState.Detached)
            {
                _context.Users.Attach(user);
                entry.State = EntityState.Modified;
            }
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
