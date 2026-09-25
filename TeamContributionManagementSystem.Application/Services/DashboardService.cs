using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;

    public DashboardService(ILogger<DashboardService> logger, IEventRepository eventRepository, IContributionRepository contributionRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? filterMonth = (month == 0 || month == null) ? null : month;
            int? filterYear = (year == 0 || year == null) ? null : year;

            var monthlyEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
            var pendingContributions = await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken);

            return new DashboardSummaryDto
            {
                MonthlyEventsCount = monthlyEvents.Count,
                TotalContributions = monthlyEvents
                    .SelectMany(x => x.Contributions)
                    .Where(x => !x.IsDeleted && x.PaymentStatus == PaymentStatus.Paid)
                    .Sum(x => x.Amount),
                PendingPayments = pendingContributions.Count,
                TotalPendingAmount = pendingContributions
                    .Where(x => !x.IsDeleted)
                    .Sum(x => x.Amount),
                UpcomingEvents = monthlyEvents.Select(x => new UpcomingEventDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventType?.EventTypeName ?? string.Empty,
                    EventDate = x.EventDate,
                    ExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount),
                    CollectedAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount),
                    PendingAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus != PaymentStatus.Paid).Sum(c => c.Amount),
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.PaymentStatus != PaymentStatus.Paid),
                    TotalContributionsCount = x.Contributions.Count(c => !c.IsDeleted),
                    CreatedBy = x.CreatedByUser != null ? x.CreatedByUser.FullName : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetSummaryAsync));
            throw;
        }
    }
}
