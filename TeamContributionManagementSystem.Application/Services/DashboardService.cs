using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
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
    private readonly ICurrentUserService? _currentUserService;
    private readonly IMemberRepository? _memberRepository;
    private readonly IExpenseRepository? _expenseRepository;

    public DashboardService(
        ILogger<DashboardService> logger,
        IEventRepository eventRepository,
        IContributionRepository contributionRepository,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null,
        IExpenseRepository? expenseRepository = null)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? filterMonth = (month == 0 || month == null) ? null : month;
            int? filterYear = (year == 0 || year == null) ? null : year;

            DateTime? startDate = null;
            DateTime? endDate = null;
            if (filterYear.HasValue && filterMonth.HasValue)
            {
                startDate = new DateTime(filterYear.Value, filterMonth.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
            }
            else if (filterYear.HasValue)
            {
                startDate = new DateTime(filterYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = new DateTime(filterYear.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            }

            var allExpensesRaw = _expenseRepository != null
                ? await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<TeamContributionManagementSystem.Application.DTOs.Expenses.ExpenseDto>();

            // Only count verified expenses on the dashboard
            var allExpenses = allExpensesRaw
                .Where(e => !string.IsNullOrWhiteSpace(e.Status) && (
                    e.Status.Contains("verif", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // If caller is in the Member role, restrict results to their own member record only
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                if (!myMemberId.HasValue && !string.IsNullOrWhiteSpace(_currentUserService.Email) && _memberRepository != null)
                {
                    var myMember = await _memberRepository.GetByEmailAsync(_currentUserService.Email.Trim(), cancellationToken);
                    if (myMember != null)
                    {
                        myMemberId = myMember.MemberId;
                    }
                }

                if (!myMemberId.HasValue)
                {
                    return new DashboardSummaryDto
                    {
                        MonthlyEventsCount = 0,
                        TotalContributions = 0,
                        PendingPayments = 0,
                        TotalPendingAmount = 0,
                        TotalExpenses = 0,
                        UpcomingEvents = Array.Empty<UpcomingEventDto>()
                    };
                }

                var memberEvents = await _eventRepository.GetAllForMemberAsync(myMemberId.Value, filterMonth, filterYear, cancellationToken);
                var allMonthlyEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);

                var memberPendingContributions = (await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken))
                    .Where(x => x.MemberId == myMemberId.Value)
                    .ToList();

                var memberEventPendingSum = memberEvents.Sum(x => x.TotalExpectedAmount - x.TotalPaidAmount);
                var memberEventPendingCount = memberEvents.Sum(x => x.PendingContributionsCount);

                var memberEventCollectedSum = memberEvents.Sum(x => x.TotalPaidAmount);
                var memberEventExpensesSum = memberEvents.Sum(x =>
                    allExpenses.Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase)).Sum(e => e.Amount)
                );

                return new DashboardSummaryDto
                {
                    MonthlyEventsCount = memberEvents.Count,
                    TotalContributions = RoundingHelper.RoundToWhole(memberEventCollectedSum),
                    PendingPayments = memberEvents.Count > 0 ? memberEventPendingCount : memberPendingContributions.Count,
                    TotalPendingAmount = RoundingHelper.RoundToWhole(memberEvents.Count > 0 ? memberEventPendingSum : memberPendingContributions.Sum(x => x.Amount)),
                    TotalExpectedAmount = RoundingHelper.RoundToWhole(memberEventCollectedSum + memberEventPendingSum),
                    TotalExpenses = RoundingHelper.RoundToWhole(memberEventExpensesSum),
                    TotalRemainingAmount = 0m,
                    UpcomingEvents = memberEvents.Select(x => {
                        var overallEvent = allMonthlyEvents.FirstOrDefault(e => e.EventId == x.EventId);
                        var eventExp = allExpenses
                            .Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                            .Sum(e => e.Amount);
                        var overallExpVal = overallEvent != null ? overallEvent.TotalExpectedAmount : x.TotalExpectedAmount;
                        return new UpcomingEventDto
                        {
                            EventId = x.EventId,
                            EventName = x.EventName,
                            EventTypeName = x.EventTypeName,
                            EventDate = x.EventDate,
                            ExpectedAmount = RoundingHelper.RoundToWhole(x.TotalExpectedAmount),
                            CollectedAmount = RoundingHelper.RoundToWhole(x.TotalPaidAmount),
                            PendingAmount = RoundingHelper.RoundToWhole(x.TotalExpectedAmount - x.TotalPaidAmount),
                            ExpenseAmount = RoundingHelper.RoundToWhole(eventExp),
                            RemainingAmount = 0m,
                            OverallExpectedAmount = RoundingHelper.RoundToWhole(overallExpVal),
                            OverallExpenseAmount = RoundingHelper.RoundToWhole(eventExp),
                            OverallRemainingAmount = 0m,
                            PendingContributionsCount = x.PendingContributionsCount,
                            TotalContributionsCount = x.ParticipantCount,
                            CreatedBy = x.CreatedBy,
                            CreatedAt = x.CreatedAt
                        };
                    }).ToList()
                };
            }

            List<EventSummaryDto> monthlyEvents;
            List<ContributionDto> pendingContributions;

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                Guid? orgUserId = null;
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
                {
                    orgUserId = parsedGuid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    orgUserId = _currentUserService.MemberId.Value;
                }
                else if (!string.IsNullOrWhiteSpace(_currentUserService.Email) && _memberRepository != null)
                {
                    var u = await _memberRepository.GetByEmailAsync(_currentUserService.Email.Trim(), cancellationToken);
                    if (u != null) orgUserId = u.MemberId;
                }

                if (orgUserId.HasValue)
                {
                    monthlyEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, filterMonth, filterYear, cancellationToken);
                    var orgEventIds = monthlyEvents.Select(e => e.EventId).ToHashSet();
                    var allPending = await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken);
                    pendingContributions = allPending.Where(p => orgEventIds.Contains(p.EventId)).ToList();
                }
                else
                {
                    monthlyEvents = new List<EventSummaryDto>();
                    pendingContributions = new List<ContributionDto>();
                }
            }
            else
            {
                // Admin: return all system data
                monthlyEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
                pendingContributions = await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken);
            }

            var eventPendingSum = monthlyEvents.Sum(x => x.TotalExpectedAmount - x.TotalPaidAmount);
            var eventPendingCount = monthlyEvents.Sum(x => x.PendingContributionsCount);

            var eventExpensesSum = monthlyEvents.Sum(x =>
                allExpenses.Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase)).Sum(e => e.Amount)
            );
            var periodExpenses = (startDate.HasValue || endDate.HasValue)
                ? allExpenses.Where(e => (!startDate.HasValue || e.ExpenseDate >= startDate.Value) && (!endDate.HasValue || e.ExpenseDate <= endDate.Value)).Sum(e => e.Amount)
                : allExpenses.Sum(e => e.Amount);
            var totalExpenses = monthlyEvents.Count > 0 ? eventExpensesSum : periodExpenses;
            var totalAllocated = monthlyEvents.Where(e => e.UseAvailableBalance).Sum(e => e.AllocatedAmount);
            var totalRecoveredDeficit = monthlyEvents
                .Where(e => e.UsePreviousEventBalance && e.PreviousEventAdjustmentType == "DeficitRecovery")
                .Sum(e => e.PreviousEventAdjustment);
            var adminOverallExpected = monthlyEvents.Sum(x => x.TotalExpectedAmount);
            var adminTotalCollections = monthlyEvents.Sum(x => x.TotalPaidAmount);
            var rawRemaining = adminTotalCollections - totalExpenses - totalAllocated;
            // Amount In Hand = rawRemaining + recoveredDeficits.
            // Do NOT clamp to zero — a negative result means a genuine uncovered shortfall.
            // Example: rawRemaining = -2600, recovered = 100 → adminOverallRemaining = -2500
            var adminOverallRemaining = (rawRemaining < 0m && totalRecoveredDeficit > 0m)
                ? rawRemaining + totalRecoveredDeficit
                : rawRemaining;

            return new DashboardSummaryDto
            {
                MonthlyEventsCount = monthlyEvents.Count,
                TotalContributions = RoundingHelper.RoundToWhole(adminTotalCollections),
                PendingPayments = monthlyEvents.Count > 0 ? eventPendingCount : pendingContributions.Count,
                TotalPendingAmount = RoundingHelper.RoundToWhole(monthlyEvents.Count > 0 ? eventPendingSum : pendingContributions.Sum(x => x.Amount)),
                TotalAllocatedAmount = RoundingHelper.RoundToWhole(totalAllocated),
                TotalRecoveredDeficitAmount = RoundingHelper.RoundToWhole(totalRecoveredDeficit),
                TotalExpectedAmount = RoundingHelper.RoundToWhole(adminOverallExpected),
                TotalExpenses = RoundingHelper.RoundToWhole(totalExpenses),
                TotalRemainingAmount = RoundingHelper.RoundToWhole(adminOverallRemaining),
                UpcomingEvents = monthlyEvents.Select(x => {
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);
                    var eventAllocated = x.UseAvailableBalance ? x.AllocatedAmount : 0m;
                    var eventRemaining = x.TotalPaidAmount - eventExp - eventAllocated;
                    return new UpcomingEventDto
                    {
                        EventId = x.EventId,
                        EventName = x.EventName,
                        EventTypeName = x.EventTypeName,
                        EventDate = x.EventDate,
                        ExpectedAmount = RoundingHelper.RoundToWhole(x.TotalExpectedAmount),
                        CollectedAmount = RoundingHelper.RoundToWhole(x.TotalPaidAmount),
                        PendingAmount = RoundingHelper.RoundToWhole(x.TotalExpectedAmount - x.TotalPaidAmount),
                        ExpenseAmount = RoundingHelper.RoundToWhole(eventExp),
                        UseAvailableBalance = x.UseAvailableBalance,
                        AllocatedAmount = x.AllocatedAmount,
                        UsePreviousEventBalance = x.UsePreviousEventBalance,
                        PreviousEventAdjustment = x.PreviousEventAdjustment,
                        PreviousEventAdjustmentType = x.PreviousEventAdjustmentType,
                        OriginalPlannedBudget = x.OriginalPlannedBudget,
                        RemainingAmount = RoundingHelper.RoundToWhole(eventRemaining),
                        OverallExpectedAmount = RoundingHelper.RoundToWhole(x.TotalExpectedAmount),
                        OverallExpenseAmount = RoundingHelper.RoundToWhole(eventExp),
                        OverallRemainingAmount = RoundingHelper.RoundToWhole(eventRemaining),
                        PendingContributionsCount = x.PendingContributionsCount,
                        TotalContributionsCount = x.ParticipantCount,
                        CreatedBy = x.CreatedBy,
                        CreatedAt = x.CreatedAt
                    };
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
