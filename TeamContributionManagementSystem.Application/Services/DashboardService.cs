using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ILogger<DashboardService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IMemberRepository? _memberRepository;

    public DashboardService(
        ILogger<DashboardService> logger,
        IEventRepository eventRepository,
        IContributionRepository contributionRepository,
        IDashboardRepository dashboardRepository,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
        _dashboardRepository = dashboardRepository;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? filterMonth = (month == 0 || month == null) ? null : month;
            int? filterYear = (year == 0 || year == null) ? null : year;

            var currentMonthSummary = await GetCurrentMonthSummaryAsync(cancellationToken);

            // If caller is in the Member role, restrict results to their own user record only
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                Guid? currentUserId = null;
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var uid))
                {
                    currentUserId = uid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    currentUserId = _currentUserService.MemberId.Value;
                }

                if (!currentUserId.HasValue)
                {
                    return new DashboardSummaryDto
                    {
                        MonthlyEventsCount = 0,
                        TotalContributions = 0,
                        PendingPayments = 0,
                        TotalPendingAmount = 0,
                        CurrentMonthSummary = currentMonthSummary,
                        UpcomingEvents = Array.Empty<UpcomingEventDto>()
                    };
                }

                var memberEvents = await _eventRepository.GetAllForMemberAsync(currentUserId.Value, filterMonth, filterYear, cancellationToken);
                var memberPendingContributions = (await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken))
                    .Where(x => x.MemberId == currentUserId.Value)
                    .ToList();

                var memberEventPendingSum = memberEvents.Sum(x => x.TotalExpectedAmount - x.TotalPaidAmount);
                var memberEventPendingCount = memberEvents.Sum(x => x.PendingContributionsCount);

                return new DashboardSummaryDto
                {
                    MonthlyEventsCount = memberEvents.Count,
                    TotalContributions = memberEvents.Sum(x => x.TotalPaidAmount),
                    PendingPayments = memberEvents.Count > 0 ? memberEventPendingCount : memberPendingContributions.Count,
                    TotalPendingAmount = memberEvents.Count > 0 ? memberEventPendingSum : memberPendingContributions.Sum(x => x.Amount),
                    CurrentMonthSummary = currentMonthSummary,
                    UpcomingEvents = memberEvents.Select(x => new UpcomingEventDto
                    {
                        EventId = x.EventId,
                        EventName = x.EventName,
                        EventTypeName = x.EventTypeName,
                        EventDate = x.EventDate,
                        ExpectedAmount = x.TotalExpectedAmount,
                        CollectedAmount = x.TotalPaidAmount,
                        PendingAmount = x.TotalExpectedAmount - x.TotalPaidAmount,
                        PendingContributionsCount = x.PendingContributionsCount,
                        TotalContributionsCount = x.ParticipantCount,
                        CreatedBy = x.CreatedBy,
                        CreatedAt = x.CreatedAt
                    }).ToList()
                };
            }

            // Admin / Organizer: return all users' aggregated data as currently
            var monthlyEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
            var pendingContributions = await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken);

            var eventPendingSum = monthlyEvents.Sum(x => x.TotalExpectedAmount - x.TotalPaidAmount);
            var eventPendingCount = monthlyEvents.Sum(x => x.PendingContributionsCount);

            return new DashboardSummaryDto
            {
                MonthlyEventsCount = monthlyEvents.Count,
                TotalContributions = monthlyEvents.Sum(x => x.TotalPaidAmount),
                PendingPayments = monthlyEvents.Count > 0 ? eventPendingCount : pendingContributions.Count,
                TotalPendingAmount = monthlyEvents.Count > 0 ? eventPendingSum : pendingContributions.Sum(x => x.Amount),
                CurrentMonthSummary = currentMonthSummary,
                UpcomingEvents = monthlyEvents.Select(x => new UpcomingEventDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventTypeName,
                    EventDate = x.EventDate,
                    ExpectedAmount = x.TotalExpectedAmount,
                    CollectedAmount = x.TotalPaidAmount,
                    PendingAmount = x.TotalExpectedAmount - x.TotalPaidAmount,
                    PendingContributionsCount = x.PendingContributionsCount,
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

    public async Task<CurrentMonthSummaryDto> GetCurrentMonthSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Guid? currentUserId = null;
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var uid))
                {
                    currentUserId = uid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    currentUserId = _currentUserService.MemberId.Value;
                }
            }

            var (totalCount, totalCollected, totalExpensed) = await _dashboardRepository.GetCurrentMonthMetricsAsync(currentUserId, cancellationToken);
            var remaining = totalCollected - totalExpensed;

            return new CurrentMonthSummaryDto
            {
                TotalCount = totalCount,
                TotalAmountCollected = totalCollected,
                TotalAmountExpensed = totalExpensed,
                RemainingAmount = remaining
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetCurrentMonthSummaryAsync));
            throw;
        }
    }
}
