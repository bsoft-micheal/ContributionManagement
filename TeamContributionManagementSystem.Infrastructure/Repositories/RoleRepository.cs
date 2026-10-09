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

            foreach (var r in roles)
            {
                var nameLower = r.RoleName.Trim().ToLower();
                // Admin and Member are protected core system roles that cannot be deleted
                r.IsReferred = nameLower == "admin" || nameLower == "member";

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
            var userRoles = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync(cancellationToken);
            if (userRoles.Count > 0)
            {
                _context.UserRoles.RemoveRange(userRoles);
            }

            var allRemainingRoles = await _context.Roles
                .Where(r => r.RoleId != roleId && !r.IsDeleted)
                .ToListAsync(cancellationToken);
            var memberRole = allRemainingRoles.FirstOrDefault(r => r.RoleName.Equals("Member", StringComparison.OrdinalIgnoreCase))
                ?? allRemainingRoles.FirstOrDefault();

            var usersWithRole = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.RoleId == roleId || (!string.IsNullOrEmpty(roleName) && u.Role.ToString().ToLower() == roleName.ToLower()))
                .ToListAsync(cancellationToken);

            foreach (var u in usersWithRole)
            {
                if (u.RoleId == roleId)
                {
                    var fallbackId = u.UserRoles.FirstOrDefault(ur => ur.RoleId != roleId && ur.IsPrimary)?.RoleId
                        ?? u.UserRoles.FirstOrDefault(ur => ur.RoleId != roleId)?.RoleId
                        ?? memberRole?.RoleId;
                    u.RoleId = fallbackId;
                }

                if (!string.IsNullOrEmpty(roleName) && u.Role.ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackName = u.UserRoles.FirstOrDefault(ur => ur.RoleId != roleId && ur.IsPrimary)?.Role?.RoleName
                        ?? u.UserRoles.FirstOrDefault(ur => ur.RoleId != roleId)?.Role?.RoleName
                        ?? "Member";
                    if (Enum.TryParse<TeamContributionManagementSystem.Domain.Enums.UserRole>(fallbackName, ignoreCase: true, out var pRole))
                    {
                        u.Role = pRole;
                    }
                    else
                    {
                        u.Role = TeamContributionManagementSystem.Domain.Enums.UserRole.Member;
                    }
                }

                var remainingCount = u.UserRoles.Count(ur => ur.RoleId != roleId);
                if (remainingCount <= 1)
                {
                    u.EnableMultipleRoles = false;
                }
            }

            var cleanName = roleName?.Trim().ToLower();
            if (!string.IsNullOrEmpty(cleanName))
            {
                var rights = await _context.RoleRights
                    .Where(rr => rr.Role != null && rr.Role.RoleName.ToLower() == cleanName)
                    .ToListAsync(cancellationToken);
                if (rights.Count > 0)
                {
                    _context.RoleRights.RemoveRange(rights);
                }
            }

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
