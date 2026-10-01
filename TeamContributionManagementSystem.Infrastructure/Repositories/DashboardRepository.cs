using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardRepository> _logger;

    public DashboardRepository(ApplicationDbContext context, ILogger<DashboardRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int TotalCount, decimal TotalCollected, decimal TotalExpensed)> GetCurrentMonthMetricsAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            int currentMonth = now.Month;
            int currentYear = now.Year;

            var activeEventsQuery = _context.Events
                .Where(e => !e.IsDeleted 
                            && e.EventDate.Month == currentMonth 
                            && e.EventDate.Year == currentYear);

            int totalCount;
            decimal totalCollected;
            decimal totalExpensed = 0m;

            if (userId.HasValue)
            {
                var myId = userId.Value;

                var myActiveEvents = await activeEventsQuery
                    .Where(e => e.Participants.Any(p => p.UserId == myId && !p.IsDeleted))
                    .ToListAsync(cancellationToken);

                totalCount = myActiveEvents.Count;
                var myEventIds = myActiveEvents.Select(e => e.EventId).ToList();

                totalCollected = await _context.Contributions
                    .Where(c => !c.IsDeleted 
                                && c.UserId == myId 
                                && c.StatusItem != null 
                                && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified") 
                                && myEventIds.Contains(c.EventId))
                    .SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;
            }
            else
            {
                var activeEvents = await activeEventsQuery.ToListAsync(cancellationToken);
                totalCount = activeEvents.Count;
                var activeEventIds = activeEvents.Select(e => e.EventId).ToList();
                var activeEventNames = activeEvents.Select(e => e.EventName.Trim().ToLower()).ToList();

                totalCollected = await _context.Contributions
                    .Where(c => !c.IsDeleted 
                                && c.StatusItem != null 
                                && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified") 
                                && activeEventIds.Contains(c.EventId))
                    .SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;

                if (activeEventNames.Count > 0)
                {
                    totalExpensed = await _context.Expenses
                        .Where(e => !e.IsDeleted 
                                    && e.ExpenseDate.Month == currentMonth 
                                    && e.ExpenseDate.Year == currentYear 
                                    && activeEventNames.Contains(e.EventName.Trim().ToLower()))
                        .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
                }
            }

            return (totalCount, totalCollected, totalExpensed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetCurrentMonthMetricsAsync));
            throw;
        }
    }
}
