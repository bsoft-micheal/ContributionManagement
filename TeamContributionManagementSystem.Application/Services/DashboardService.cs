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
                TotalContributions = monthlyEvents.Sum(x => x.TotalPaidAmount),
                PendingPayments = pendingContributions.Count,
                TotalPendingAmount = pendingContributions.Sum(x => x.Amount),
                UpcomingEvents = monthlyEvents.Select(x => new UpcomingEventDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventTypeName,
                    EventDate = x.EventDate,
                    ExpectedAmount = x.TotalExpectedAmount,
                    CollectedAmount = x.TotalPaidAmount,
                    PendingAmount = x.TotalExpectedAmount - x.TotalPaidAmount,
                    PendingContributionsCount = 0,
                    TotalContributionsCount = x.ParticipantCount,
                    CreatedBy = x.CreatedBy,
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
