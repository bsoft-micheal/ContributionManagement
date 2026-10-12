using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(ApplicationDbContext context, ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var memberRoleIds = await _context.UserRoles
                .Where(ur => ur.User != null && !ur.User.IsDeleted)
                .Select(ur => ur.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var userRoleNames = await _context.UserRoles
                .Where(ur => ur.User != null && !ur.User.IsDeleted && ur.Role != null)
                .Select(ur => ur.Role!.RoleName.ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);

            var roleRightNames = await _context.RoleRights
                .Select(r => r.Role.ToString().ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var roles = await _context.Roles
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.RoleName)
                .Select(x => new RoleDto
                {
                    RoleId = x.RoleId,
                    RoleName = x.RoleName,
                    DefaultContributionAmount = 0,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            var assignedRoleIds = await _context.UserRoles
                .Where(ur => ur.User == null || !ur.User.IsDeleted)
                .Select(ur => ur.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var assignedRoleSet = new HashSet<Guid>(assignedRoleIds);

            foreach (var r in roles)
            {
                var nameLower = r.RoleName.Trim().ToLower();
                // Admin and Member are protected core system roles that cannot be deleted.
                // Also mark as referred if any active user currently has this role assigned.
                r.IsReferred = nameLower == "admin" || nameLower == "member" || assignedRoleSet.Contains(r.RoleId);

                if (!string.IsNullOrWhiteSpace(r.CreatedBy) && Guid.TryParse(r.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    r.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(r.ModifiedBy) && Guid.TryParse(r.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
                {
                    r.ModifiedBy = mName;
                }
            }

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.RoleName.ToLower() == roleName.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByNameAsync));
            throw;
        }
    }

    public async Task<bool> HasMembersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles.AnyAsync(x => x.RoleId == roleId && x.User != null && !x.User.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasMembersAsync));
            throw;
        }
    }

    public async Task<bool> HasUsersAsync(Guid roleId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles.AnyAsync(ur => ur.RoleId == roleId && ur.User != null && !ur.User.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasUsersAsync));
            throw;
        }
    }

    public async Task<bool> HasRoleRightsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanName = roleName.Trim().ToLower();
            return await _context.RoleRights.AnyAsync(x => x.Role != null && x.Role.RoleName.ToLower() == cleanName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasRoleRightsAsync));
            throw;
        }
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Roles.AddAsync(role, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(Role role)
    {
        try
        {
            _context.Roles.Update(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(Role role)
    {
        try
        {
            role.IsDeleted = true;
            _context.Roles.Update(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }

    public async Task DeleteRoleWithAssignmentsAsync(Guid roleId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Remove UserRole mappings for this role
            var userRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync(cancellationToken);

            var affectedUserIds = userRoles.Select(ur => ur.UserId).Distinct().ToList();

            if (userRoles.Count > 0)
            {
                _context.UserRoles.RemoveRange(userRoles);
            }

            // 2. Adjust multiple roles flag for affected users if needed
            if (affectedUserIds.Count > 0)
            {
                var affectedUsers = await _context.Users
                    .Include(u => u.UserRoles)
                    .Where(u => affectedUserIds.Contains(u.UserId))
                    .ToListAsync(cancellationToken);

                foreach (var u in affectedUsers)
                {
                    var remainingCount = u.UserRoles.Count(ur => ur.RoleId != roleId);
                    if (remainingCount <= 1)
                    {
                        u.EnableMultipleRoles = false;
                    }
                }
            }

            // 3. Remove permissions/rights associated with this role
            var cleanName = roleName?.Trim().ToLower();
            var rights = await _context.RoleRights
                .Where(rr => rr.RoleId == roleId || (!string.IsNullOrEmpty(cleanName) && rr.Role != null && rr.Role.RoleName.ToLower() == cleanName))
                .ToListAsync(cancellationToken);
            if (rights.Count > 0)
            {
                _context.RoleRights.RemoveRange(rights);
            }

            // 4. Soft delete the role record
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
            if (role != null)
            {
                role.IsDeleted = true;
                _context.Roles.Update(role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteRoleWithAssignmentsAsync));
            throw;
        }
    }
}
