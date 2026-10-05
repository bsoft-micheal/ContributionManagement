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

            var allExpenses = _expenseRepository != null
                ? await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<TeamContributionManagementSystem.Application.DTOs.Expenses.ExpenseDto>();

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

                var memberEventExpensesSum = allMonthlyEvents.Sum(x =>
                    allExpenses.Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase)).Sum(e => e.Amount)
                );
                var memberPeriodExpenses = (startDate.HasValue || endDate.HasValue)
                    ? allExpenses.Where(e => (!startDate.HasValue || e.ExpenseDate >= startDate.Value) && (!endDate.HasValue || e.ExpenseDate <= endDate.Value)).Sum(e => e.Amount)
                    : allExpenses.Sum(e => e.Amount);
                var overallExpenses = allMonthlyEvents.Count > 0 ? memberEventExpensesSum : memberPeriodExpenses;
                var overallExpected = allMonthlyEvents.Sum(x => x.TotalExpectedAmount);
                var overallRemaining = overallExpected - overallExpenses;

                return new DashboardSummaryDto
                {
                    MonthlyEventsCount = memberEvents.Count,
                    TotalContributions = memberEvents.Sum(x => x.TotalPaidAmount),
                    PendingPayments = memberEvents.Count > 0 ? memberEventPendingCount : memberPendingContributions.Count,
                    TotalPendingAmount = memberEvents.Count > 0 ? memberEventPendingSum : memberPendingContributions.Sum(x => x.Amount),
                    TotalExpectedAmount = memberEvents.Sum(x => x.TotalExpectedAmount),
                    TotalExpenses = overallExpenses,
                    TotalRemainingAmount = overallRemaining,
                    UpcomingEvents = memberEvents.Select(x => {
                        var overallEvent = allMonthlyEvents.FirstOrDefault(e => e.EventId == x.EventId);
                        var eventExp = allExpenses
                            .Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                            .Sum(e => e.Amount);
                        var overallExpVal = overallEvent != null ? overallEvent.TotalExpectedAmount : x.TotalExpectedAmount;
                        var eventRemaining = overallExpVal - eventExp;
                        return new UpcomingEventDto
                        {
                            EventId = x.EventId,
                            EventName = x.EventName,
                            EventTypeName = x.EventTypeName,
                            EventDate = x.EventDate,
                            ExpectedAmount = x.TotalExpectedAmount,
                            CollectedAmount = x.TotalPaidAmount,
                            PendingAmount = x.TotalExpectedAmount - x.TotalPaidAmount,
                            ExpenseAmount = eventExp,
                            RemainingAmount = eventRemaining,
                            OverallExpectedAmount = overallExpVal,
                            OverallExpenseAmount = eventExp,
                            OverallRemainingAmount = eventRemaining,
                            PendingContributionsCount = x.PendingContributionsCount,
                            TotalContributionsCount = x.ParticipantCount,
                            CreatedBy = x.CreatedBy,
                            CreatedAt = x.CreatedAt
                        };
                    }).ToList()
                };
            }

            // Admin / Organizer: return all users' aggregated data as currently
            var monthlyEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
            var pendingContributions = await _contributionRepository.GetPendingAsync(filterMonth, filterYear, cancellationToken);

            var eventPendingSum = monthlyEvents.Sum(x => x.TotalExpectedAmount - x.TotalPaidAmount);
            var eventPendingCount = monthlyEvents.Sum(x => x.PendingContributionsCount);

            var eventExpensesSum = monthlyEvents.Sum(x =>
                allExpenses.Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase)).Sum(e => e.Amount)
            );
            var periodExpenses = (startDate.HasValue || endDate.HasValue)
                ? allExpenses.Where(e => (!startDate.HasValue || e.ExpenseDate >= startDate.Value) && (!endDate.HasValue || e.ExpenseDate <= endDate.Value)).Sum(e => e.Amount)
                : allExpenses.Sum(e => e.Amount);
            var totalExpenses = monthlyEvents.Count > 0 ? eventExpensesSum : periodExpenses;
            var adminOverallExpected = monthlyEvents.Sum(x => x.TotalExpectedAmount);
            var adminOverallRemaining = adminOverallExpected - totalExpenses;

            return new DashboardSummaryDto
            {
                MonthlyEventsCount = monthlyEvents.Count,
                TotalContributions = monthlyEvents.Sum(x => x.TotalPaidAmount),
                PendingPayments = monthlyEvents.Count > 0 ? eventPendingCount : pendingContributions.Count,
                TotalPendingAmount = monthlyEvents.Count > 0 ? eventPendingSum : pendingContributions.Sum(x => x.Amount),
                TotalExpectedAmount = adminOverallExpected,
                TotalExpenses = totalExpenses,
                TotalRemainingAmount = adminOverallRemaining,
                UpcomingEvents = monthlyEvents.Select(x => {
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), x.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);
                    var eventRemaining = x.TotalExpectedAmount - eventExp;
                    return new UpcomingEventDto
                    {
                        EventId = x.EventId,
                        EventName = x.EventName,
                        EventTypeName = x.EventTypeName,
                        EventDate = x.EventDate,
                        ExpectedAmount = x.TotalExpectedAmount,
                        CollectedAmount = x.TotalPaidAmount,
                        PendingAmount = x.TotalExpectedAmount - x.TotalPaidAmount,
                        ExpenseAmount = eventExp,
                        RemainingAmount = eventRemaining,
                        OverallExpectedAmount = x.TotalExpectedAmount,
                        OverallExpenseAmount = eventExp,
                        OverallRemainingAmount = eventRemaining,
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
