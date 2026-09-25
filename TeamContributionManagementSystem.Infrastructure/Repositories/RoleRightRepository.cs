using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
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

    public async Task<List<RoleRight>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var navMenus = await _context.NavigationMenus.OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.ToListAsync(cancellationToken);
            var rightsMap = rights.ToDictionary(x => $"{x.Role}|{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}", StringComparer.OrdinalIgnoreCase);

            var parentMap = navMenus.Where(m => m.ParentID == 0).ToDictionary(m => m.FeatureID);

            var result = new List<RoleRight>();
            foreach (var role in Enum.GetValues<UserRole>())
            {
                result.AddRange(BuildRightsFromNavigation(role, navMenus, parentMap, rightsMap));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<RoleRight>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var navMenus = await _context.NavigationMenus.OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
            var rights = await _context.RoleRights.Where(x => x.Role == role).ToListAsync(cancellationToken);
            var rightsMap = rights.ToDictionary(x => $"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}", StringComparer.OrdinalIgnoreCase);

            var parentMap = navMenus.Where(m => m.ParentID == 0).ToDictionary(m => m.FeatureID);

            return BuildRightsFromNavigation(role, navMenus, parentMap, rightsMap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByRoleAsync));
            throw;
        }
    }

    private static List<RoleRight> BuildRightsFromNavigation(
        UserRole role,
        List<NavigationMenu> navMenus,
        Dictionary<int, NavigationMenu> parentMap,
        Dictionary<string, RoleRight> rightsMap)
    {
        var result = new List<RoleRight>();

        foreach (var menu in navMenus)
        {
            if (!menu.ShowingUserRight) continue;

            string moduleName;
            string subModuleName;
            string pageName;

            if (menu.ParentID == 0)
            {
                moduleName = menu.Module ?? string.Empty;
                subModuleName = string.Empty;
                pageName = menu.Module ?? string.Empty;
            }
            else
            {
                parentMap.TryGetValue(menu.ParentID, out var parent);
                moduleName = parent?.Module ?? string.Empty;
                subModuleName = menu.SubModule ?? string.Empty;
                pageName = menu.SubModule ?? menu.Activity ?? string.Empty;
            }

            var key = $"{moduleName.Trim()}|{subModuleName.Trim()}|{pageName.Trim()}";
            var mapKey = rightsMap.ContainsKey(key) ? key : $"{role}|{key}";

            RoleRight? existing = null;
            if (rightsMap.TryGetValue(mapKey, out existing) || rightsMap.TryGetValue(key, out existing))
            {
                result.Add(new RoleRight
                {
                    RoleRightId = existing.RoleRightId,
                    Role = role,
                    FeatureID = menu.FeatureID,
                    Module = moduleName,
                    SubModule = subModuleName,
                    Page = pageName,
                    Access = existing.Access,
                    AccessType = (int)existing.AccessType > 0 ? existing.AccessType : (existing.Access == "deny" ? AccessType.Deny : (existing.Access == "readOnly" ? AccessType.ReadOnly : AccessType.ReadWrite)),
                    CreatedBy = existing.CreatedBy,
                    CreatedAt = existing.CreatedAt
                });
            }
            else
            {
                // Default access fallback if not yet stored in DB
                AccessType defaultAccessType = (role == UserRole.Admin || role == UserRole.Organizer) ? AccessType.ReadWrite : AccessType.ReadOnly;
                string defaultAccess = defaultAccessType == AccessType.ReadWrite ? "readWrite" : "readOnly";
                result.Add(new RoleRight
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role,
                    FeatureID = menu.FeatureID,
                    Module = moduleName,
                    SubModule = subModuleName,
                    Page = pageName,
                    Access = defaultAccess,
                    AccessType = defaultAccessType
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
                .GroupBy(r => new { Module = r.Module.Trim(), SubModule = r.SubModule.Trim(), Page = r.Page.Trim() })
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
