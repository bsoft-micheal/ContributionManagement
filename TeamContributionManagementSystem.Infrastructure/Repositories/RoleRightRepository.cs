using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class RoleRightRepository : IRoleRightRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<RoleRightRepository> _logger;

    public RoleRightRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<RoleRightRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoleRight>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RoleRights.OrderBy(x => x.Role).ThenBy(x => x.Module).ThenBy(x => x.Page).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<List<RoleRight>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RoleRights.Where(x => x.Role == role).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByRoleAsync");
            throw;
        }
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

        var existingMap = existing.ToDictionary(
            x => $"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}",
            StringComparer.OrdinalIgnoreCase);

        var incomingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var right in rightsList)
        {
            var key = $"{right.Module.Trim()}|{right.SubModule.Trim()}|{right.Page.Trim()}";
            incomingKeys.Add(key);

            if (existingMap.TryGetValue(key, out var existingRight))
            {
                existingRight.Access = right.Access;
            }
            else
            {
                await _context.RoleRights.AddAsync(new RoleRight
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role,
                    Module = right.Module.Trim(),
                    SubModule = right.SubModule.Trim(),
                    Page = right.Page.Trim(),
                    Access = right.Access
                }, cancellationToken);
            }
        }

        var toDelete = existing
            .Where(x => !incomingKeys.Contains($"{x.Module.Trim()}|{x.SubModule.Trim()}|{x.Page.Trim()}"))
            .ToList();

        if (toDelete.Count > 0)
        {
            _context.RoleRights.RemoveRange(toDelete);
        }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SaveRoleRightsAsync");
            throw;
        }
    }
}
