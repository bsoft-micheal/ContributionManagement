using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly ApplicationDbContext _context;

    public StatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, string? module = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Statuses
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(module) && !module.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Module != null && x.Module.ToLower() == module.ToLower());
        }

        return await query
            .OrderBy(x => x.Module)
            .ThenBy(x => x.StatusName)
            .Select(x => new StatusDto
            {
                StatusId = x.StatusId,
                StatusName = x.StatusName,
                Module = x.Module ?? "General",
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken)
    {
        return GetAllAsync(activeOnly, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        var navModules = await _context.NavigationMenus
            .Where(x => !string.IsNullOrWhiteSpace(x.Module))
            .Select(x => x.Module!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var navSubModules = await _context.NavigationMenus
            .Where(x => !string.IsNullOrWhiteSpace(x.Activity) && x.Activity != "#")
            .Select(x => x.Activity!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var statusModules = await _context.Statuses
            .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Module))
            .Select(x => x.Module!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var defaultModules = new List<string>
        {
            "Support Ticket",
            "Expense",
            "Events",
            "Contributions / Payments",
            "Members",
            "Exit Process",
            "Budget Calculations",
            "General"
        };

        var allModules = navModules
            .Concat(navSubModules)
            .Concat(statusModules)
            .Concat(defaultModules)
            .Where(m => !string.IsNullOrWhiteSpace(m) && m != "Dashboard")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        return allModules;
    }

    public async Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusName.ToLower() == name.ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default)
    {
        var query = _context.Statuses.Where(x => x.StatusName.ToLower() == name.ToLower() && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module != null && x.Module.ToLower() == module.ToLower());
        }
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Status status, CancellationToken cancellationToken = default)
    {
        await _context.Statuses.AddAsync(status, cancellationToken);
    }

    public void Update(Status status)
    {
        _context.Statuses.Update(status);
    }

    public void Delete(Status status)
    {
        status.IsDeleted = true;
        _context.Statuses.Update(status);
    }
}
