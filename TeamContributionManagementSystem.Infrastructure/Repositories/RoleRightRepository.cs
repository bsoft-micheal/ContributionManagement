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
            var navMenus = await _context.NavigationMenus.ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.ToListAsync(cancellationToken);
            var rightsByRoleAndFeature = rights
                .Where(x => x.FeatureID > 0)
                .GroupBy(x => x.Role)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.FeatureID));
            var rightsMap = rights.ToDictionary(x => $"{x.Role}|{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}", StringComparer.OrdinalIgnoreCase);

            var allMenuMap = navMenus.ToDictionary(m => m.FeatureID);

            var result = new List<RoleRightDto>();
            foreach (var role in Enum.GetValues<UserRole>())
            {
                var roleFeatures = rightsByRoleAndFeature.TryGetValue(role, out var map) ? map : new Dictionary<int, RoleRight>();
                result.AddRange(BuildRightsFromNavigation(role, navMenus, allMenuMap, roleFeatures, rightsMap));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<RoleRightDto>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var navMenus = await _context.NavigationMenus.ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.Where(x => x.Role == role).ToListAsync(cancellationToken);
            var rightsByFeature = rights.Where(x => x.FeatureID > 0).ToDictionary(x => x.FeatureID);
            var rightsMap = rights.ToDictionary(x => $"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}", StringComparer.OrdinalIgnoreCase);

            var allMenuMap = navMenus.ToDictionary(m => m.FeatureID);

            return BuildRightsFromNavigation(role, navMenus, allMenuMap, rightsByFeature, rightsMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByRoleAsync));
            throw;
        }
    }

    private static List<RoleRightDto> BuildRightsFromNavigation(
        UserRole role,
        List<NavigationMenu> navMenus,
        Dictionary<int, NavigationMenu> allMenuMap,
        Dictionary<int, RoleRight> rightsByFeature,
        Dictionary<string, RoleRight> rightsMap)
    {
        var result = new List<RoleRightDto>();

        // Build hierarchical ordering: Parent (MenuType 1) -> SubModule (MenuType 2) -> Action (MenuType 3)
        var orderedNavMenus = new List<NavigationMenu>();
        var rootMenus = navMenus.Where(m => m.ParentID == 0).OrderBy(m => m.MainModuleID).ThenBy(m => m.FeatureID).ToList();
        var childrenByParent = navMenus.Where(m => m.ParentID != 0).GroupBy(m => m.ParentID).ToDictionary(g => g.Key, g => g.OrderBy(m => m.FeatureID).ToList());

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

            var key = $"{moduleName.Trim()}|{subModuleName.Trim()}|{pageName.Trim()}";
            var mapKey = rightsMap.ContainsKey(key) ? key : $"{role}|{key}";

            RoleRight? existing = null;
            if ((menu.FeatureID > 0 && rightsByFeature.TryGetValue(menu.FeatureID, out existing))
                || rightsMap.TryGetValue(mapKey, out existing)
                || rightsMap.TryGetValue(key, out existing))
            {
                result.Add(new RoleRightDto
                {
                    RoleRightId = existing.RoleRightId,
                    Role = role.ToString(),
                    FeatureID = menu.FeatureID,
                    Module = moduleName,
                    SubModule = subModuleName,
                    Action = actionName,
                    Page = pageName,
                    MenuType = menuType,
                    Access = existing.Access,
                    AccessType = (int)existing.AccessType > 0 ? (int)existing.AccessType : (existing.Access == "deny" ? (int)AccessType.Deny : (existing.Access == "readOnly" ? (int)AccessType.ReadOnly : (int)AccessType.ReadWrite)),
                    CreatedBy = existing.CreatedBy,
                    CreatedAt = existing.CreatedAt,
                    CreatedOn = existing.CreatedOn ?? existing.CreatedAt
                });
            }
            else
            {
                // Default access fallback if not yet stored in DB
                AccessType defaultAccessType = (role == UserRole.Admin || role == UserRole.Organizer) ? AccessType.ReadWrite : AccessType.ReadOnly;
                string defaultAccess = defaultAccessType == AccessType.ReadWrite ? "readWrite" : "readOnly";
                result.Add(new RoleRightDto
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role.ToString(),
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

    public async Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.RoleRights.Where(x => x.Role == role).ToListAsync(cancellationToken);
            var rightsList = rights
                .GroupBy(r => r.FeatureID > 0 ? (object)r.FeatureID : $"{r.Module.Trim()}|{r.SubModule.Trim()}|{r.Page.Trim()}")
                .Select(g => g.First())
                .ToList();

            var rightsByFeature = existing.Where(x => x.FeatureID > 0).ToDictionary(x => x.FeatureID);
            var existingMap = existing.ToDictionary(
                x => $"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}",
                StringComparer.OrdinalIgnoreCase);

            var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var incomingFeatureIds = new HashSet<int>();

            foreach (var right in rightsList)
            {
                var key = $"{right.Module.Trim()}|{right.SubModule.Trim()}|{right.Page.Trim()}";
                incomingKeys.Add(key);
                if (right.FeatureID > 0) incomingFeatureIds.Add(right.FeatureID);

                // Determine effective AccessType & string Access
                AccessType effectiveAccessType = (int)right.AccessType > 0 ? right.AccessType : (right.Access == "deny" ? AccessType.Deny : (right.Access == "readOnly" ? AccessType.ReadOnly : AccessType.ReadWrite));
                string effectiveAccess = effectiveAccessType == AccessType.Deny ? "deny" : (effectiveAccessType == AccessType.ReadOnly ? "readOnly" : "readWrite");

                RoleRight? existingRight = null;
                if (right.FeatureID > 0 && rightsByFeature.TryGetValue(right.FeatureID, out existingRight))
                {
                    existingRight.Access = effectiveAccess;
                    existingRight.AccessType = effectiveAccessType;
                    existingRight.Module = right.Module.Trim();
                    existingRight.SubModule = right.SubModule.Trim();
                    existingRight.Page = right.Page.Trim();
                }
                else if (existingMap.TryGetValue(key, out existingRight))
                {
                    existingRight.Access = effectiveAccess;
                    existingRight.AccessType = effectiveAccessType;
                    existingRight.FeatureID = right.FeatureID;
                    existingRight.Module = right.Module.Trim();
                    existingRight.SubModule = right.SubModule.Trim();
                    existingRight.Page = right.Page.Trim();
                }
                else
                {
                    await _context.RoleRights.AddAsync(new RoleRight
                    {
                        RoleRightId = Guid.NewGuid(),
                        Role = role,
                        FeatureID = right.FeatureID,
                        Module = right.Module.Trim(),
                        SubModule = right.SubModule.Trim(),
                        Page = right.Page.Trim(),
                        Access = effectiveAccess,
                        AccessType = effectiveAccessType
                    }, cancellationToken);
                }
            }

            var toDelete = existing
                .Where(x => (x.FeatureID > 0 && !incomingFeatureIds.Contains(x.FeatureID)) &&
                            !incomingKeys.Contains($"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}"))
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
}
