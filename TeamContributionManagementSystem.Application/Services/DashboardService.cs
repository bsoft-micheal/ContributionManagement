using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;

    public DashboardService(IEventRepository eventRepository, IContributionRepository contributionRepository)
    {
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var targetDate = new DateTime(year ?? DateTime.UtcNow.Year, month ?? DateTime.UtcNow.Month, 1);
        var monthlyEvents = await _eventRepository.GetAllAsync(targetDate.Month, targetDate.Year, cancellationToken);
        var pendingContributions = await _contributionRepository.GetPendingAsync(targetDate.Month, targetDate.Year, cancellationToken);
        var upcomingEvents = await _eventRepository.GetUpcomingAsync(5, cancellationToken);

        return new DashboardSummaryDto
        {
            MonthlyEventsCount = monthlyEvents.Count,
            TotalContributions = monthlyEvents
                .SelectMany(x => x.Contributions)
                .Where(x => !x.IsDeleted && x.PaymentStatus == PaymentStatus.Paid)
                .Sum(x => x.Amount),
            PendingPayments = pendingContributions.Count,
            UpcomingEvents = upcomingEvents.Select(x => new UpcomingEventDto
            {
                EventId = x.EventId,
                EventName = x.EventName,
                EventTypeName = x.EventType?.EventTypeName ?? string.Empty,
                EventDate = x.EventDate,
                ExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount)
            }).ToList()
        };
    }
}
