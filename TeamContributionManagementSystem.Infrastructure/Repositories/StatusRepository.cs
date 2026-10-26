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

    public Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        return GetAllAsync(activeOnly, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly, string? module, CancellationToken cancellationToken = default)
    {
        var query = _context.Statuses
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            var cleanModule = module.Trim().ToLower();
            var singular = cleanModule.EndsWith("s") && cleanModule.Length > 3 ? cleanModule.Substring(0, cleanModule.Length - 1) : cleanModule;
            var plural = cleanModule.EndsWith("s") ? cleanModule : cleanModule + "s";

            query = query.Where(x =>
                x.Module == null ||
                x.Module.ToLower() == cleanModule ||
                x.Module.ToLower() == singular ||
                x.Module.ToLower() == plural ||
                x.Module.ToLower().Contains(cleanModule) ||
                x.Module.ToLower().Contains(singular) ||
                x.Module.ToLower().Contains(plural) ||
                x.Module.ToLower() == "general");
        }

        var usedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var ticketStatuses = await _context.SupportTickets
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
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
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
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
            .Where(x => !x.IsDeleted && x.StatusItem != null)
            .Select(x => x.StatusItem!.StatusName.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var s in contributionStatuses) usedStatuses.Add(s);

        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
            .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

        var items = await query
            .OrderBy(x => x.StatusName)
            .Select(x => new StatusDto
            {
                StatusId = x.StatusId,
                StatusName = x.StatusName,
                Module = x.Module ?? "General",
                ModuleId = x.ModuleId,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var cleanName = item.StatusName.Trim().ToLower();
            item.IsReferred = usedStatuses.Contains(cleanName);

            if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
            {
                item.CreatedBy = cName;
            }
            if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
            {
                item.ModifiedBy = mName;
            }
        }

        return items;
    }

    public async Task<IReadOnlyCollection<string>> GetAllModuleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .Where(x => !x.IsDeleted && !string.IsNullOrWhiteSpace(x.Module))
            .Select(x => x.Module!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllModuleAsync(cancellationToken);
    }

    public async Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusName.ToLower() == name.Trim().ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAndModuleAsync(string name, string? module, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var cleanName = name.Trim().ToLower();
        var query = _context.Statuses.Where(x => x.StatusName.ToLower() == cleanName && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(module))
        {
            var cleanModule = module.Trim().ToLower();
            var singular = cleanModule.EndsWith("s") && cleanModule.Length > 3 ? cleanModule.Substring(0, cleanModule.Length - 1) : cleanModule;
            var plural = cleanModule.EndsWith("s") ? cleanModule : cleanModule + "s";
            query = query.Where(x =>
                x.Module == null ||
                x.Module.ToLower() == cleanModule ||
                x.Module.ToLower() == singular ||
                x.Module.ToLower() == plural ||
                x.Module.ToLower().Contains(cleanModule) ||
                x.Module.ToLower().Contains(singular) ||
                x.Module.ToLower().Contains(plural) ||
                x.Module.ToLower() == "general");
        }
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsInUseAsync(string statusName, CancellationToken cancellationToken = default)
    {
        var cleanName = statusName.Trim().ToLower();

        // Support tickets
        var inSupport = await _context.SupportTickets.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inSupport) return true;

        // Expenses
        var inExpenses = await _context.Expenses.AnyAsync(x => !x.IsDeleted && x.Status.ToLower() == cleanName, cancellationToken);
        if (inExpenses) return true;

        // Payment Transactions
        var inTxns = await _context.PaymentTransactions.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inTxns) return true;

        // Events
        if (Enum.TryParse<TeamContributionManagementSystem.Domain.Enums.EventStatus>(cleanName, true, out var eventStatus))
        {
            var inEvents = await _context.Events.AnyAsync(x => !x.IsDeleted && x.Status == eventStatus, cancellationToken);
            if (inEvents) return true;
        }

        // Contributions
        var inContributions = await _context.Contributions.AnyAsync(x => !x.IsDeleted && x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanName, cancellationToken);
        if (inContributions) return true;

        return false;
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
