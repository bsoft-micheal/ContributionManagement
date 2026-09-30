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

            var roles = await _context.Roles
                .OrderBy(x => x.RoleName)
                .Select(x => new RoleDto
                {
                    RoleId = x.RoleId,
                    RoleName = x.RoleName,
                    DefaultContributionAmount = 0,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var r in roles)
            {
                var nameLower = r.RoleName.Trim().ToLower();
                r.IsReferred = memberRoleIds.Contains(r.RoleId) || userRoleNames.Contains(nameLower) || roleRightNames.Contains(nameLower);
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
            var cleanName = roleName.Trim();
            if (Enum.TryParse<TeamContributionManagementSystem.Domain.Enums.UserRole>(cleanName, true, out var parsedRole))
            {
                return await _context.RoleRights.AnyAsync(x => x.Role == parsedRole, cancellationToken);
            }
            return false;
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
            _context.Roles.Remove(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }
}
