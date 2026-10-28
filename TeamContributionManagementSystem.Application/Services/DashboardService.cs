using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Payments;
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
    private readonly IPaymentTransactionRepository? _paymentTransactionRepository;
    private readonly IUserRepository? _userRepository;
    private readonly IPaymentTransactionService? _paymentTransactionService;
    private readonly IEventTypeRepository? _eventTypeRepository;

    public DashboardService(
        ILogger<DashboardService> logger,
        IEventRepository eventRepository,
        IContributionRepository contributionRepository,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null,
        IExpenseRepository? expenseRepository = null,
        IPaymentTransactionRepository? paymentTransactionRepository = null,
        IUserRepository? userRepository = null,
        IPaymentTransactionService? paymentTransactionService = null,
        IEventTypeRepository? eventTypeRepository = null)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
        _expenseRepository = expenseRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _userRepository = userRepository;
        _paymentTransactionService = paymentTransactionService;
        _eventTypeRepository = eventTypeRepository;
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

            var isOrganizer = _currentUserService != null && _currentUserService.IsOrganizerRole;

            if (isOrganizer)
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

                    // Restrict expenses strictly to events created by this organizer
                    var allOrgEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, cancellationToken: cancellationToken);
                    var orgEventNames = allOrgEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    allExpenses = allExpenses
                        .Where(e => !string.IsNullOrWhiteSpace(e.EventName) && orgEventNames.Contains(e.EventName.Trim().ToLower()))
                        .ToList();
                }
                else
                {
                    monthlyEvents = new List<EventSummaryDto>();
                    pendingContributions = new List<ContributionDto>();
                    allExpenses = new List<TeamContributionManagementSystem.Application.DTOs.Expenses.ExpenseDto>();
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
            var totalExpenses = isOrganizer ? eventExpensesSum : (monthlyEvents.Count > 0 ? eventExpensesSum : periodExpenses);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("GetSummaryAsync request was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetSummaryAsync));
            throw;
        }
    }

    private async Task<(Guid? MemberId, string MemberName, string Email)> ResolveCurrentMemberAsync(CancellationToken cancellationToken)
    {
        if (_currentUserService == null) return (null, string.Empty, string.Empty);

        var memberId = _currentUserService.MemberId;
        string memberName = string.Empty;
        string email = _currentUserService.Email ?? string.Empty;

        if (memberId.HasValue && _memberRepository != null)
        {
            var member = await _memberRepository.GetByIdAsync(memberId.Value, cancellationToken);
            if (member != null)
            {
                return (member.MemberId, member.Name, member.Email);
            }
        }

        if (!string.IsNullOrWhiteSpace(email) && _memberRepository != null)
        {
            var member = await _memberRepository.GetByEmailAsync(email.Trim(), cancellationToken);
            if (member != null)
            {
                return (member.MemberId, member.Name, member.Email);
            }
        }

        if (memberId.HasValue && _userRepository != null)
        {
            var user = await _userRepository.GetByIdAsync(memberId.Value, cancellationToken);
            if (user != null)
            {
                var name = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Username ?? string.Empty);
                return (user.UserId, name, user.Email);
            }
        }

        if (!string.IsNullOrWhiteSpace(_currentUserService.UserName))
        {
            memberName = _currentUserService.UserName;
        }

        return (memberId, memberName, email);
    }

    public async Task<MemberDashboardSummaryDto> GetMemberSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (memberId, memberName, email) = await ResolveCurrentMemberAsync(cancellationToken);
            if (!memberId.HasValue)
            {
                return new MemberDashboardSummaryDto
                {
                    MemberName = memberName ?? string.Empty,
                    SelectedMonth = month,
                    SelectedYear = year
                };
            }

            int? filterMonth = (month == 0 || month == null) ? null : month;
            int? filterYear = (year == 0 || year == null) ? null : year;

            var memberEvents = await _eventRepository.GetAllForMemberAsync(memberId.Value, filterMonth, filterYear, cancellationToken);

            var allTxns = _paymentTransactionRepository != null
                ? await _paymentTransactionRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<PaymentTransactionDto>();

            var myTxns = allTxns
                .Where(t => (t.UserId.HasValue && t.UserId.Value == memberId.Value) ||
                            (!string.IsNullOrWhiteSpace(t.MemberName) && string.Equals(t.MemberName.Trim(), memberName.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var allContribs = await _contributionRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
            var myContribs = allContribs.Where(c => c.MemberId == memberId.Value).ToList();

            decimal totalPaid = 0m;
            decimal totalPending = 0m;
            int paidEventsCount = 0;
            int pendingEventsCount = 0;

            foreach (var ev in memberEvents)
            {
                decimal expected = ev.TotalExpectedAmount > 0 ? ev.TotalExpectedAmount : ev.BaseAmount;

                var evTxns = myTxns.Where(t =>
                    (t.EventId.HasValue && t.EventId.Value == ev.EventId) ||
                    (!string.IsNullOrWhiteSpace(t.EventName) && string.Equals(t.EventName.Trim(), ev.EventName.Trim(), StringComparison.OrdinalIgnoreCase))
                ).ToList();

                decimal verifiedPaid = evTxns
                    .Where(t => string.Equals(t.Status, "Verified", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .Sum(t => t.Amount);

                var contrib = myContribs.FirstOrDefault(c => c.EventId == ev.EventId);
                if (contrib != null && (
                    string.Equals(contrib.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Closed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Completed", StringComparison.OrdinalIgnoreCase)))
                {
                    verifiedPaid = Math.Max(verifiedPaid, contrib.Amount);
                }
                else if (verifiedPaid == 0 && ev.TotalPaidAmount > 0)
                {
                    verifiedPaid = ev.TotalPaidAmount;
                }

                decimal pending = Math.Max(0, expected - verifiedPaid);

                totalPaid += verifiedPaid;
                totalPending += pending;

                if (pending == 0 && expected > 0)
                {
                    paidEventsCount++;
                }
                else
                {
                    pendingEventsCount++;
                }
            }

            return new MemberDashboardSummaryDto
            {
                TotalPaid = RoundingHelper.RoundToWhole(totalPaid),
                TotalPending = RoundingHelper.RoundToWhole(totalPending),
                TotalEvents = memberEvents.Count,
                PaidEventsCount = paidEventsCount,
                PendingEventsCount = pendingEventsCount,
                MemberName = memberName ?? string.Empty,
                SelectedMonth = filterMonth,
                SelectedYear = filterYear
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("GetMemberSummaryAsync request was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMemberSummaryAsync));
            throw;
        }
    }

    public async Task<IReadOnlyCollection<MemberContributionItemDto>> GetMemberContributionsAsync(int? month = null, int? year = null, Guid? eventTypeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (memberId, memberName, email) = await ResolveCurrentMemberAsync(cancellationToken);
            if (!memberId.HasValue)
            {
                return Array.Empty<MemberContributionItemDto>();
            }

            int? filterMonth = (month == 0 || month == null) ? null : month;
            int? filterYear = (year == 0 || year == null) ? null : year;

            var memberEvents = await _eventRepository.GetAllForMemberAsync(memberId.Value, filterMonth, filterYear, cancellationToken);
            if (eventTypeId.HasValue && eventTypeId.Value != Guid.Empty)
            {
                memberEvents = memberEvents.Where(e => e.EventTypeId == eventTypeId.Value).ToList();
            }

            var allTxns = _paymentTransactionRepository != null
                ? await _paymentTransactionRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<PaymentTransactionDto>();

            var myTxns = allTxns
                .Where(t => (t.UserId.HasValue && t.UserId.Value == memberId.Value) ||
                            (!string.IsNullOrWhiteSpace(t.MemberName) && string.Equals(t.MemberName.Trim(), memberName.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var allContribs = await _contributionRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
            var myContribs = allContribs.Where(c => c.MemberId == memberId.Value).ToList();

            var items = new List<MemberContributionItemDto>();

            foreach (var ev in memberEvents)
            {
                decimal expected = ev.TotalExpectedAmount > 0 ? ev.TotalExpectedAmount : ev.BaseAmount;

                var evTxns = myTxns.Where(t =>
                    (t.EventId.HasValue && t.EventId.Value == ev.EventId) ||
                    (!string.IsNullOrWhiteSpace(t.EventName) && string.Equals(t.EventName.Trim(), ev.EventName.Trim(), StringComparison.OrdinalIgnoreCase))
                ).ToList();

                decimal verifiedPaid = evTxns
                    .Where(t => string.Equals(t.Status, "Verified", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .Sum(t => t.Amount);

                var contrib = myContribs.FirstOrDefault(c => c.EventId == ev.EventId);
                if (contrib != null && (
                    string.Equals(contrib.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Closed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contrib.StatusName, "Completed", StringComparison.OrdinalIgnoreCase)))
                {
                    verifiedPaid = Math.Max(verifiedPaid, contrib.Amount);
                }
                else if (verifiedPaid == 0 && ev.TotalPaidAmount > 0)
                {
                    verifiedPaid = ev.TotalPaidAmount;
                }

                decimal pending = Math.Max(0, expected - verifiedPaid);

                // Check for any pending verification transactions for this event
                var hasPendingTxn = evTxns.Any(t =>
                    string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.Status, "Pending Verification", StringComparison.OrdinalIgnoreCase) ||
                    (string.IsNullOrWhiteSpace(t.VerifiedBy) &&
                     !string.Equals(t.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(t.Status, "Verified", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(t.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                );

                var latestTxn = evTxns
                    .OrderByDescending(t => t.CreatedAt ?? t.PaymentDate)
                    .ThenByDescending(t => t.PaymentDate)
                    .FirstOrDefault();

                string paymentStatus = "Unpaid";
                string verificationStatus = "None";
                bool canPay = true;

                if (hasPendingTxn)
                {
                    paymentStatus = "Pending Verification";
                    verificationStatus = "Pending";
                    canPay = false;
                }
                else if (latestTxn != null)
                {
                    var st = (latestTxn.Status ?? string.Empty).Trim().ToLower();
                    if (st == "rejected")
                    {
                        paymentStatus = "Payment Rejected";
                        verificationStatus = "Rejected";
                        canPay = true;
                    }
                    else if (st == "verified" || st == "approved" || st == "paid" || st == "completed")
                    {
                        verificationStatus = "Verified";
                        if (verifiedPaid >= expected && expected > 0)
                        {
                            paymentStatus = "Paid";
                            canPay = false;
                        }
                        else
                        {
                            paymentStatus = "Partially Paid";
                            canPay = true;
                        }
                    }
                }
                else
                {
                    if (contrib != null && (
                        string.Equals(contrib.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contrib.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contrib.StatusName, "Approved", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contrib.StatusName, "Closed", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(contrib.StatusName, "Completed", StringComparison.OrdinalIgnoreCase)))
                    {
                        paymentStatus = "Paid";
                        verificationStatus = "Verified";
                        canPay = false;
                    }
                    else if (verifiedPaid > 0)
                    {
                        paymentStatus = verifiedPaid >= expected ? "Paid" : "Partially Paid";
                        verificationStatus = "Verified";
                        canPay = verifiedPaid < expected;
                    }
                    else
                    {
                        paymentStatus = "Unpaid";
                        verificationStatus = "None";
                        canPay = true;
                    }
                }

                // List down all events (both paid and unpaid) in My Contributions
                decimal previousArrears = 0m;
                if (_paymentTransactionService != null)
                {
                    try
                    {
                        var context = await _paymentTransactionService.GetPaymentContextAsync(ev.EventId, memberId.Value, ev.EventName, memberName, cancellationToken);
                        if (context != null)
                        {
                            previousArrears = context.PreviousArrears;
                        }
                    }
                    catch { }
                }

                decimal remainingBalance = pending > 0 ? pending : expected;

                items.Add(new MemberContributionItemDto
                {
                    EventId = ev.EventId,
                    EventTypeId = ev.EventTypeId,
                    EventTypeName = !string.IsNullOrWhiteSpace(ev.EventTypeName) ? ev.EventTypeName : "Event",
                    EventName = !string.IsNullOrWhiteSpace(ev.EventName) ? ev.EventName : "Event",
                    EventDate = ev.EventDate,
                    CreatedAt = ev.CreatedAt ?? ev.CreatedOn ?? ev.EventDate,
                    ContributionAmount = RoundingHelper.RoundToWhole(paymentStatus == "Partially Paid" ? remainingBalance : expected),
                    VerifiedPaidAmount = RoundingHelper.RoundToWhole(verifiedPaid),
                    OutstandingAmount = RoundingHelper.RoundToWhole(pending),
                    PreviousArrears = RoundingHelper.RoundToWhole(previousArrears),
                    TotalDue = RoundingHelper.RoundToWhole(pending + previousArrears),
                    PaymentVerificationStatus = verificationStatus,
                    PaymentStatus = paymentStatus,
                    PaymentEligibility = canPay,
                    ContributionId = contrib?.ContributionId,
                    MemberId = memberId.Value,
                    MemberName = memberName ?? string.Empty
                });
            }

            int GetStatusOrder(string status)
            {
                var s = (status ?? string.Empty).Trim().ToLowerInvariant();
                if (s == "unpaid") return 1;
                if (s == "partially paid" || s == "partial") return 2;
                if (s == "pending verification" || s == "pending") return 3;
                if (s == "payment rejected" || s == "rejected") return 4;
                if (s == "paid" || s == "approved" || s == "verified") return 5;
                return 6;
            }

            return items
                .OrderBy(x => GetStatusOrder(x.PaymentStatus))
                .ThenByDescending(x => x.CreatedAt ?? x.EventDate)
                .ThenByDescending(x => x.EventDate)
                .ThenByDescending(x => x.EventId)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("GetMemberContributionsAsync request was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMemberContributionsAsync));
            throw;
        }
    }

    public async Task<IReadOnlyCollection<MemberRecentPaymentDto>> GetMemberRecentPaymentsAsync(int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var (memberId, memberName, email) = await ResolveCurrentMemberAsync(cancellationToken);
            if (!memberId.HasValue)
            {
                return Array.Empty<MemberRecentPaymentDto>();
            }

            var allTxns = _paymentTransactionRepository != null
                ? await _paymentTransactionRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<PaymentTransactionDto>();

            var myTxns = allTxns
                .Where(t => ((t.UserId.HasValue && t.UserId.Value == memberId.Value) ||
                             (!string.IsNullOrWhiteSpace(t.MemberName) && string.Equals(t.MemberName.Trim(), memberName.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                            (string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(t.Status, "Verified", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(t.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(t.Status, "Paid", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(t => t.PaymentDate)
                .ThenByDescending(t => t.CreatedAt ?? DateTime.MinValue)
                .ToList();

            var eventDict = (await _eventRepository.GetAllAsync(null, null, cancellationToken))
                .ToDictionary(e => e.EventId, e => e.EventTypeName);

            var allContribs = await _contributionRepository.GetAllAsync(null, null, cancellationToken);
            var myPaidContribs = allContribs.Where(c => c.MemberId == memberId.Value && (
                string.Equals(c.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.StatusName, "Approved", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.StatusName, "Completed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.StatusName, "Closed", StringComparison.OrdinalIgnoreCase)) &&
                !allTxns.Any(t => ((t.EventId.HasValue && t.EventId.Value == c.EventId) ||
                                  (!string.IsNullOrWhiteSpace(t.EventName) && !string.IsNullOrWhiteSpace(c.EventName) && string.Equals(t.EventName.Trim(), c.EventName.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                                  ((t.UserId.HasValue && t.UserId.Value == memberId.Value) ||
                                  (!string.IsNullOrWhiteSpace(t.MemberName) && string.Equals(t.MemberName.Trim(), memberName.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                                  (string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(t.Status, "Pending Verification", StringComparison.OrdinalIgnoreCase) ||
                                   string.IsNullOrWhiteSpace(t.VerifiedBy)))).ToList();

            var list = new List<MemberRecentPaymentDto>();
            var seenEventIds = new HashSet<Guid>();

            foreach (var t in myTxns)
            {
                if (t.EventId.HasValue)
                {
                    seenEventIds.Add(t.EventId.Value);
                }

                string evType = string.Empty;
                if (t.EventId.HasValue && eventDict.TryGetValue(t.EventId.Value, out var typeName))
                {
                    evType = typeName ?? string.Empty;
                }

                list.Add(new MemberRecentPaymentDto
                {
                    PaymentId = t.TransactionId,
                    TxnNumber = t.TxnNumber ?? string.Empty,
                    EventId = t.EventId,
                    EventName = !string.IsNullOrWhiteSpace(t.EventName) ? t.EventName : "Event Payment",
                    EventTypeName = evType,
                    PaymentDate = t.PaymentDate,
                    PaymentAmount = RoundingHelper.RoundToWhole(t.Amount),
                    PaymentMode = !string.IsNullOrWhiteSpace(t.PaymentMode) ? t.PaymentMode : "UPI",
                    VerificationStatus = "Approved"
                });
            }

            foreach (var c in myPaidContribs)
            {
                if (!seenEventIds.Contains(c.EventId))
                {
                    seenEventIds.Add(c.EventId);
                    string evType = !string.IsNullOrWhiteSpace(c.CategoryName) ? c.CategoryName : string.Empty;
                    if (string.IsNullOrWhiteSpace(evType) && eventDict.TryGetValue(c.EventId, out var typeName))
                    {
                        evType = typeName ?? string.Empty;
                    }

                    list.Add(new MemberRecentPaymentDto
                    {
                        PaymentId = c.ContributionId,
                        TxnNumber = string.Empty,
                        EventId = c.EventId,
                        EventName = !string.IsNullOrWhiteSpace(c.EventName) ? c.EventName : "Event Payment",
                        EventTypeName = evType,
                        PaymentDate = c.PaymentDate ?? c.CreatedAt ?? DateTime.UtcNow,
                        PaymentAmount = RoundingHelper.RoundToWhole(c.Amount),
                        PaymentMode = c.PaymentMode.ToString(),
                        VerificationStatus = "Approved"
                    });
                }
            }

            return list
                .OrderByDescending(t => t.PaymentDate)
                .Take(limit)
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("GetMemberRecentPaymentsAsync request was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMemberRecentPaymentsAsync));
            throw;
        }
    }
}
