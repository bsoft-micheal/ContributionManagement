using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class RoleRightRepository : IRoleRightRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleRightRepository> _logger;

    public RoleRightRepository(ApplicationDbContext context, ILogger<RoleRightRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _context.Roles.Where(r => !r.IsDeleted).ToListAsync(cancellationToken);
            var navMenus = await _context.NavigationMenus.ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.ToListAsync(cancellationToken);

            var rightsByRoleAndFeature = rights
                .Where(x => x.FeatureID > 0)
                .GroupBy(x => x.RoleId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.FeatureID));

            var allMenuMap = navMenus.ToDictionary(m => m.FeatureID);

            var result = new List<RoleRightDto>();
            foreach (var role in roles)
            {
                var roleFeatures = rightsByRoleAndFeature.TryGetValue(role.RoleId, out var map) ? map : new Dictionary<int, RoleRight>();
                result.AddRange(BuildRightsFromNavigation(role.RoleId, role.RoleName, navMenus, allMenuMap, roleFeatures));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<RoleRightDto>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
            var roleName = role?.RoleName ?? "Role";

            var navMenus = await _context.NavigationMenus.ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
            var rightsByFeature = rights.Where(x => x.FeatureID > 0).ToDictionary(x => x.FeatureID);

            var allMenuMap = navMenus.ToDictionary(m => m.FeatureID);

            return BuildRightsFromNavigation(roleId, roleName, navMenus, allMenuMap, rightsByFeature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByRoleIdAsync));
            throw;
        }
    }

    public async Task<List<RoleRightDto>> GetByRoleNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedName = roleName?.Trim() ?? string.Empty;
            var role = await _context.Roles.FirstOrDefaultAsync(r => EF.Functions.ILike(r.RoleName, trimmedName), cancellationToken);
            if (role == null)
            {
                if (Guid.TryParse(trimmedName, out var roleGuid))
                {
                    return await GetByRoleIdAsync(roleGuid, cancellationToken);
                }
                return new List<RoleRightDto>();
            }

            return await GetByRoleIdAsync(role.RoleId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByRoleNameAsync));
            throw;
        }
    }

    public async Task<List<RoleRightDto>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await GetByRoleNameAsync(role.ToString(), cancellationToken);
    }

    private static List<RoleRightDto> BuildRightsFromNavigation(
        Guid roleId,
        string roleName,
        List<NavigationMenu> navMenus,
        Dictionary<int, NavigationMenu> allMenuMap,
        Dictionary<int, RoleRight> rightsByFeature)
    {
        var result = new List<RoleRightDto>();

        // Build hierarchical ordering: Parent (MenuType 1) -> SubModule (MenuType 2) -> Action (MenuType 3)
        var orderedNavMenus = new List<NavigationMenu>();
        var rootMenus = navMenus.Where(m => m.ParentID == 0).OrderBy(m => m.MainModuleID).ThenBy(m => m.DisplayOrder).ThenBy(m => m.FeatureID).ToList();
        var childrenByParent = navMenus.Where(m => m.ParentID != 0).GroupBy(m => m.ParentID).ToDictionary(g => g.Key, g => g.OrderBy(m => m.DisplayOrder).ThenBy(m => m.FeatureID).ToList());

        foreach (var root in rootMenus)
        {
            orderedNavMenus.Add(root);
            if (childrenByParent.TryGetValue(root.FeatureID, out var level2List))
            {
                foreach (var l2 in level2List)
                {
                    orderedNavMenus.Add(l2);
                    if (childrenByParent.TryGetValue(l2.FeatureID, out var level3List))
                    {
                        foreach (var l3 in level3List)
                        {
                            orderedNavMenus.Add(l3);
                        }
                    }
                }
            }
        }

        // Add any remaining menus not caught by tree hierarchy
        var addedIds = new HashSet<int>(orderedNavMenus.Select(m => m.FeatureID));
        foreach (var remaining in navMenus.Where(m => !addedIds.Contains(m.FeatureID)).OrderBy(m => m.DisplayOrder).ThenBy(m => m.FeatureID))
        {
            orderedNavMenus.Add(remaining);
        }

        foreach (var menu in orderedNavMenus)
        {
            if (!menu.ShowingUserRight) continue;

            string moduleName;
            string subModuleName;
            string actionName;
            string pageName;
            int menuType = menu.MenuType;

            if (menu.ParentID == 0)
            {
                moduleName = menu.Module ?? menu.Activity ?? string.Empty;
                subModuleName = string.Empty;
                actionName = string.Empty;
                pageName = moduleName;
                menuType = 1;
            }
            else
            {
                allMenuMap.TryGetValue(menu.ParentID, out var parent);

                if (menu.MenuType == 3)
                {
                    // Action level
                    if (parent != null && parent.ParentID != 0)
                    {
                        allMenuMap.TryGetValue(parent.ParentID, out var grandParent);
                        moduleName = grandParent?.Module ?? grandParent?.Activity ?? parent?.Module ?? string.Empty;
                        subModuleName = !string.IsNullOrWhiteSpace(parent?.Activity) ? parent.Activity : (parent?.SubModule ?? string.Empty);
                    }
                    else
                    {
                        moduleName = parent?.Module ?? parent?.Activity ?? string.Empty;
                        subModuleName = string.Empty;
                    }
                    actionName = !string.IsNullOrWhiteSpace(menu.SubModule) ? menu.SubModule : (menu.Activity ?? string.Empty);
                    pageName = !string.IsNullOrWhiteSpace(actionName) ? actionName : (!string.IsNullOrWhiteSpace(subModuleName) ? subModuleName : moduleName);
                }
                else
                {
                    // SubModule / Page level (MenuType == 2)
                    moduleName = parent?.Module ?? parent?.Activity ?? string.Empty;
                    subModuleName = !string.IsNullOrWhiteSpace(menu.Activity) ? menu.Activity : (menu.SubModule ?? string.Empty);
                    actionName = string.Empty;
                    pageName = !string.IsNullOrWhiteSpace(subModuleName) ? subModuleName : moduleName;
                    menuType = 2;
                }
            }

            RoleRight? existing = null;
            if (rightsByFeature.TryGetValue(menu.FeatureID, out existing))
            {
                result.Add(new RoleRightDto
                {
                    RoleRightId = existing.RoleRightId,
                    RoleId = roleId,
                    Role = roleName,
                    FeatureID = menu.FeatureID,
                    Module = moduleName,
                    SubModule = subModuleName,
                    Action = actionName,
                    Page = pageName,
                    MenuType = menuType,
                    Access = existing.Access,
                    AccessType = (int)existing.AccessType > 0 ? (int)existing.AccessType : (existing.Access == "deny" ? (int)AccessType.Deny : (existing.Access == "readOnly" ? (int)AccessType.ReadOnly : (int)AccessType.ReadWrite)),
                    CreatedBy = existing.CreatedBy.HasValue ? existing.CreatedBy.Value.ToString() : null,
                    CreatedAt = existing.CreatedAt,
                    CreatedOn = existing.CreatedOn ?? existing.CreatedAt
                });
            }
            else
            {
                // Default access fallback if not yet stored in DB
                bool isAdminOrOrg = roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Organizer", StringComparison.OrdinalIgnoreCase);
                AccessType defaultAccessType = isAdminOrOrg ? AccessType.ReadWrite : AccessType.ReadOnly;
                string defaultAccess = defaultAccessType == AccessType.ReadWrite ? "readWrite" : "readOnly";
                result.Add(new RoleRightDto
                {
                    RoleRightId = Guid.NewGuid(),
                    RoleId = roleId,
                    Role = roleName,
                    FeatureID = menu.FeatureID,
                    Module = moduleName,
                    SubModule = subModuleName,
                    Action = actionName,
                    Page = pageName,
                    MenuType = menuType,
                    Access = defaultAccess,
                    AccessType = (int)defaultAccessType,
                    CreatedAt = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        return result;
    }

    public async Task SaveRoleRightsAsync(Guid roleId, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.RoleRights.Where(x => x.RoleId == roleId).ToListAsync(cancellationToken);
            var rightsList = rights
                .Where(r => r.FeatureID > 0)
                .GroupBy(r => r.FeatureID)
                .Select(g => g.First())
                .ToList();

            var rightsByFeature = existing.Where(x => x.FeatureID > 0).ToDictionary(x => x.FeatureID);
            var incomingFeatureIds = new HashSet<int>();

            foreach (var right in rightsList)
            {
                incomingFeatureIds.Add(right.FeatureID);

                // Determine effective AccessType & string Access
                AccessType effectiveAccessType = (int)right.AccessType > 0 ? right.AccessType : (right.Access == "deny" ? AccessType.Deny : (right.Access == "readOnly" ? AccessType.ReadOnly : AccessType.ReadWrite));
                string effectiveAccess = effectiveAccessType == AccessType.Deny ? "deny" : (effectiveAccessType == AccessType.ReadOnly ? "readOnly" : "readWrite");

                if (rightsByFeature.TryGetValue(right.FeatureID, out var existingRight))
                {
                    existingRight.Access = effectiveAccess;
                    existingRight.AccessType = effectiveAccessType;
                    existingRight.ModifiedOn = DateTime.UtcNow;
                }
                else
                {
                    await _context.RoleRights.AddAsync(new RoleRight
                    {
                        RoleRightId = Guid.NewGuid(),
                        RoleId = roleId,
                        FeatureID = right.FeatureID,
                        Access = effectiveAccess,
                        AccessType = effectiveAccessType,
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    }, cancellationToken);
                }
            }

            var toDelete = existing
                .Where(x => !incomingFeatureIds.Contains(x.FeatureID))
                .ToList();

            if (toDelete.Count > 0)
            {
                _context.RoleRights.RemoveRange(toDelete);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(SaveRoleRightsAsync));
            throw;
        }
    }

    public async Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default)
    {
        var roleName = role.ToString();
        var roleEntity = await _context.Roles.FirstOrDefaultAsync(r => EF.Functions.ILike(r.RoleName, roleName), cancellationToken);
        if (roleEntity == null)
        {
            throw new InvalidOperationException($"Role {role} not found in database.");
        }
        await SaveRoleRightsAsync(roleEntity.RoleId, rights, cancellationToken);
    }
}
