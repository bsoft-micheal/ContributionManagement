using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly ApplicationDbContext _context;
    private static bool _moduleColumnEnsured = false;
    private static readonly SemaphoreSlim _columnLock = new(1, 1);

    public StatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private async Task EnsureModuleColumnAsync(CancellationToken cancellationToken = default)
    {
        if (_moduleColumnEnsured) return;
        await _columnLock.WaitAsync(cancellationToken);
        try
        {
            if (_moduleColumnEnsured) return;
            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    @"ALTER TABLE IF EXISTS statuses ADD COLUMN IF NOT EXISTS module VARCHAR(100) NULL;
                      ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_key;
                      ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS statuses_status_name_unique;
                      ALTER TABLE IF EXISTS statuses DROP CONSTRAINT IF EXISTS uq_statuses_status_name;",
                    cancellationToken);
            }
            catch
            {
                // ignore if already exists, insufficient permissions, or during concurrent operations
            }
            _moduleColumnEnsured = true;
        }
        finally
        {
            _columnLock.Release();
        }
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, string? module = null, CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);

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

        var usedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ticketStatuses = await _context.SupportTickets
            .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Status))
            .Select(x => x.Status.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in ticketStatuses) usedStatuses.Add(s);

        var expenseStatuses = await _context.Expenses
            .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Status))
            .Select(x => x.Status.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in expenseStatuses) usedStatuses.Add(s);

        var txnStatuses = await _context.PaymentTransactions
            .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.Status))
            .Select(x => x.Status.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in txnStatuses) usedStatuses.Add(s);

        var eventStatuses = await _context.Events
            .Where(x => !x.IsDeleted)
            .Select(x => x.Status.ToString().ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in eventStatuses) usedStatuses.Add(s);

        var contributionStatuses = await _context.Contributions
            .Where(x => !x.IsDeleted)
            .Select(x => x.PaymentStatus.ToString().ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in contributionStatuses) usedStatuses.Add(s);

        var items = await query
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

        foreach (var item in items)
        {
            var cleanName = item.StatusName.Trim().ToLower();
            item.IsReferred = usedStatuses.Contains(cleanName);
        }

        return items;
    }

    public Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken)
    {
        return GetAllAsync(activeOnly, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);

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
        await EnsureModuleColumnAsync(cancellationToken);
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusName.ToLower() == name.ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);
        var query = _context.Statuses.Where(x => x.StatusName.ToLower() == name.ToLower() && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module != null && x.Module.ToLower() == module.ToLower());
        }
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(string statusName, string? module = null, CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);
        var cleanName = statusName.Trim().ToLower();

        // Support tickets
        var inSupport = await _context.SupportTickets.AnyAsync(x => !x.IsDeleted && x.Status.ToLower() == cleanName, cancellationToken);
        if (inSupport) return true;

        // Expenses
        var inExpenses = await _context.Expenses.AnyAsync(x => !x.IsDeleted && x.Status.ToLower() == cleanName, cancellationToken);
        if (inExpenses) return true;

        // Payment Transactions
        var inTxns = await _context.PaymentTransactions.AnyAsync(x => !x.IsDeleted && x.Status.ToLower() == cleanName, cancellationToken);
        if (inTxns) return true;

        // Events
        var inEvents = await _context.Events.AnyAsync(x => !x.IsDeleted && x.Status.ToString().ToLower() == cleanName, cancellationToken);
        if (inEvents) return true;

        // Contributions
        var inContributions = await _context.Contributions.AnyAsync(x => !x.IsDeleted && x.PaymentStatus.ToString().ToLower() == cleanName, cancellationToken);
        if (inContributions) return true;

        return false;
    }

    public async Task AddAsync(Status status, CancellationToken cancellationToken = default)
    {
        await EnsureModuleColumnAsync(cancellationToken);
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
