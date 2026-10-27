using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;
    private readonly IUserRepository? _userRepository;
    private readonly ICurrentUserService? _currentUserService;

    public ReportService(
        ILogger<ReportService> logger, 
        IEventRepository eventRepository, 
        IContributionRepository contributionRepository,
        IExpenseRepository expenseRepository,
        IPaymentTransactionRepository paymentTransactionRepository,
        IUserRepository? userRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
        _expenseRepository = expenseRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? targetMonth = (month == 0 || month == null) ? null : month;
            int? targetYear = (year == 0 || year == null) ? null : year;

            List<EventSummaryDto> events;
            var pendingDues = await _contributionRepository.GetPendingAsync(targetMonth, targetYear, cancellationToken);
            var contributions = await _contributionRepository.GetAllAsync(targetMonth, targetYear, cancellationToken);

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

                if (orgUserId.HasValue)
                {
                    events = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, targetMonth, targetYear, cancellationToken);
                    var orgEventIds = events.Select(e => e.EventId).ToHashSet();
                    pendingDues = pendingDues.Where(p => orgEventIds.Contains(p.EventId)).ToList();
                    contributions = contributions.Where(c => orgEventIds.Contains(c.EventId)).ToList();
                }
                else
                {
                    events = new List<EventSummaryDto>();
                    pendingDues = new List<ContributionDto>();
                    contributions = new List<ContributionDto>();
                }
            }
            else
            {
                events = await _eventRepository.GetAllAsync(targetMonth, targetYear, cancellationToken);
            }

            // Date filtering for expenses & payments
            DateTime? startDate = null;
            DateTime? endDate = null;
            if (targetYear.HasValue && targetMonth.HasValue)
            {
                startDate = new DateTime(targetYear.Value, targetMonth.Value, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
            }
            else if (targetYear.HasValue)
            {
                startDate = new DateTime(targetYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = new DateTime(targetYear.Value, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            }

            var allExpensesRaw = await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken);
            // Only count verified expenses in reports
            var allExpenses = allExpensesRaw
                .Where(e => !string.IsNullOrWhiteSpace(e.Status) && (
                    e.Status.Contains("verif", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                var orgEventNames = events.Select(e => e.EventName.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                allExpenses = allExpenses
                    .Where(e => !string.IsNullOrWhiteSpace(e.EventName) && orgEventNames.Contains(e.EventName.Trim().ToLower()))
                    .ToList();
            }

            var allPayments = await _paymentTransactionRepository.GetAllAsync(startDate: startDate, endDate: endDate, cancellationToken: cancellationToken);

            // 1. Event Collections Report
            var eventCollections = events.Select(x =>
            {
                var expected = x.TotalExpectedAmount;
                var paid = x.TotalPaidAmount;
                var pending = expected - paid;
                var rate = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;
                var eventExp = allExpenses
                    .Where(e => !string.IsNullOrWhiteSpace(e.EventName) && e.EventName.Trim().Equals(x.EventName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(e => e.Amount);

                return new EventCollectionReportDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventTypeName,
                    EventDate = x.EventDate,
                    ExpectedAmount = RoundingHelper.RoundToWhole(expected),
                    PaidAmount = RoundingHelper.RoundToWhole(paid),
                    PendingAmount = RoundingHelper.RoundToWhole(pending),
                    CollectionRate = rate,
                    ExpenseAmount = RoundingHelper.RoundToWhole(eventExp),
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                };
            }).OrderByDescending(x => x.EventDate).ToList();

            var eventLookup = events.ToDictionary(e => e.EventId, e => e.EventDate);
            var eventTypeNameLookup = events.Where(e => !string.IsNullOrWhiteSpace(e.EventTypeName))
                .GroupBy(e => e.EventId)
                .ToDictionary(g => g.Key, g => g.First().EventTypeName);
            var eventNameLookup = events.Where(e => !string.IsNullOrWhiteSpace(e.EventName))
                .GroupBy(e => e.EventId)
                .ToDictionary(g => g.Key, g => g.First().EventName);

            // 2. Member Contributions History Report
            var memberHistory = contributions
                .GroupBy(x => x.MemberId)
                .Select(group =>
                {
                    var expected = group.Sum(x => x.Amount);
                    var paid = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount);
                    var completion = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;
                    var firstItem = group.FirstOrDefault();
                    var memberName = group.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.MemberName))?.MemberName ?? "Unknown";

                    var memberEvents = group.Select(c =>
                    {
                        var isPaid = c.PaymentStatus == PaymentStatus.Paid;
                        var evDate = eventLookup.TryGetValue(c.EventId, out var ed) ? ed : (c.CreatedAt ?? DateTime.MinValue);
                        var evName = !string.IsNullOrWhiteSpace(c.EventName) ? c.EventName : (eventNameLookup.TryGetValue(c.EventId, out var en) ? en : "Event");
                        var evType = !string.IsNullOrWhiteSpace(c.CategoryName) ? c.CategoryName : (eventTypeNameLookup.TryGetValue(c.EventId, out var et) ? et : "General");
                        return new MemberEventDetailDto
                        {
                            ContributionId = c.ContributionId,
                            EventId = c.EventId,
                            EventName = evName,
                            CategoryName = evType,
                            EventDate = evDate,
                            ExpectedAmount = RoundingHelper.RoundToWhole(c.Amount),
                            PaidAmount = isPaid ? RoundingHelper.RoundToWhole(c.Amount) : 0m,
                            PendingAmount = isPaid ? 0m : RoundingHelper.RoundToWhole(c.Amount),
                            PaymentStatus = isPaid ? "Paid" : "Pending",
                            PaymentDate = c.PaymentDate,
                            PaymentMode = c.PaymentMode.ToString()
                        };
                    }).OrderByDescending(e => e.EventDate).ToList();

                    return new MemberContributionHistoryDto
                    {
                        MemberId = group.Key,
                        MemberName = memberName,
                        TotalExpectedAmount = RoundingHelper.RoundToWhole(expected),
                        TotalPaidAmount = RoundingHelper.RoundToWhole(paid),
                        PaidEventsCount = group.Count(x => x.PaymentStatus == PaymentStatus.Paid),
                        PendingEventsCount = group.Count(x => x.PaymentStatus != PaymentStatus.Paid),
                        CompletionRate = completion,
                        CreatedBy = firstItem?.CreatedBy,
                        CreatedAt = firstItem?.CreatedAt,
                        Events = memberEvents
                    };
                })
                .OrderBy(x => x.MemberName)
                .ToList();

            // 3. Pending Dues & Defaulters Report
            var pendingDuesReport = pendingDues.Select(x =>
            {
                var days = (x.CreatedOn.HasValue || x.CreatedAt.HasValue) 
                    ? Math.Max(0, (DateTime.UtcNow - (x.CreatedOn ?? x.CreatedAt!.Value)).Days)
                    : 0;
                var aging = days > 30 
                    ? CommonConstants.AgingCategories.Critical 
                    : (days >= 15 ? CommonConstants.AgingCategories.Moderate : CommonConstants.AgingCategories.Recent);

                var eventDate = eventLookup.TryGetValue(x.EventId, out var evDate) ? evDate : (x.CreatedAt ?? DateTime.MinValue);

                return new PendingDueDto
                {
                    ContributionId = x.ContributionId,
                    MemberId = x.MemberId,
                    MemberName = x.MemberName,
                    Phone = string.Empty,
                    EventName = x.EventName,
                    EventDate = eventDate,
                    Amount = x.Amount,
                    DaysOverdue = days,
                    AgingCategory = aging,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt ?? x.CreatedOn
                };
            }).OrderByDescending(x => x.DaysOverdue).ToList();

            // 4. Event Financials (Budget vs Actual Expenses)
            var eventFinancials = events.Select(x =>
            {
                var collections = x.TotalPaidAmount;
                var expenses = allExpenses
                    .Where(e => !string.IsNullOrWhiteSpace(e.EventName) && e.EventName.Trim().Equals(x.EventName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(e => e.Amount);
                var net = collections - expenses;
                var savingsRate = collections > 0 ? Math.Round((net / collections) * 100, 1) : 0;

                return new EventFinancialReportDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventTypeName,
                    EventDate = x.EventDate,
                    TotalCollections = RoundingHelper.RoundToWhole(collections),
                    TotalExpenses = RoundingHelper.RoundToWhole(expenses),
                    NetBalance = RoundingHelper.RoundToWhole(net),
                    Status = net >= 0 ? CommonConstants.FinancialStatus.Surplus : CommonConstants.FinancialStatus.Deficit,
                    SavingsRatePercent = savingsRate,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                };
            }).OrderByDescending(x => x.EventDate).ToList();

            // 5. Payment Mode Breakdown
            var paidContributions = contributions.Where(c => c.PaymentStatus == PaymentStatus.Paid).ToList();
            var totalPaidFromContributions = paidContributions.Sum(c => c.Amount);

            var modeTotals = new Dictionary<string, (decimal TotalAmount, int Count)>();

            foreach (var c in paidContributions)
            {
                if (c.PaymentMode == PaymentMode.Split && (c.CashAmount.HasValue || c.UpiAmount.HasValue))
                {
                    var cash = c.CashAmount ?? 0;
                    var upi = c.UpiAmount ?? 0;

                    if (cash > 0)
                    {
                        if (!modeTotals.ContainsKey(CommonConstants.PaymentModes.Cash)) modeTotals[CommonConstants.PaymentModes.Cash] = (0, 0);
                        var cur = modeTotals[CommonConstants.PaymentModes.Cash];
                        modeTotals[CommonConstants.PaymentModes.Cash] = (cur.TotalAmount + cash, cur.Count + 1);
                    }
                    if (upi > 0)
                    {
                        if (!modeTotals.ContainsKey(CommonConstants.PaymentModes.Upi)) modeTotals[CommonConstants.PaymentModes.Upi] = (0, 0);
                        var cur = modeTotals[CommonConstants.PaymentModes.Upi];
                        modeTotals[CommonConstants.PaymentModes.Upi] = (cur.TotalAmount + upi, cur.Count + 1);
                    }
                }
                else
                {
                    var modeName = c.PaymentMode == PaymentMode.None ? CommonConstants.PaymentModes.Upi : (c.PaymentMode == PaymentMode.Upi ? CommonConstants.PaymentModes.Upi : c.PaymentMode.ToString());
                    if (!modeTotals.ContainsKey(modeName)) modeTotals[modeName] = (0, 0);
                    var cur = modeTotals[modeName];
                    modeTotals[modeName] = (cur.TotalAmount + c.Amount, cur.Count + 1);
                }
            }

            var paymentModeGroups = modeTotals
                .Select(kvp => new PaymentModeReportDto
                {
                    PaymentMode = kvp.Key,
                    TransactionCount = kvp.Value.Count,
                    TotalAmount = RoundingHelper.RoundToWhole(kvp.Value.TotalAmount),
                    Percentage = totalPaidFromContributions > 0 ? Math.Round((kvp.Value.TotalAmount / totalPaidFromContributions) * 100, 1) : 0,
                    VerifiedCount = kvp.Value.Count
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            if (!paymentModeGroups.Any() && allPayments.Any())
            {
                var totalFromPayments = allPayments.Sum(p => p.Amount);
                paymentModeGroups = allPayments
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.PaymentMode) ? CommonConstants.PaymentModes.Upi : p.PaymentMode)
                    .Select(g => new PaymentModeReportDto
                    {
                        PaymentMode = g.Key,
                        TransactionCount = g.Count(),
                        TotalAmount = RoundingHelper.RoundToWhole(g.Sum(p => p.Amount)),
                        Percentage = totalFromPayments > 0 ? Math.Round((g.Sum(p => p.Amount) / totalFromPayments) * 100, 1) : 0,
                        VerifiedCount = g.Count(p => p.Status == CommonConstants.PaymentStatuses.Verified || p.Status == CommonConstants.PaymentStatuses.Approved)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();
            }

            // High-Level Financial Summary KPIs
            var totalExpected = contributions.Sum(c => c.Amount);
            var totalCollected = paidContributions.Sum(c => c.Amount);
            var totalExp = allExpenses.Sum(e => e.Amount);
            var totalPending = pendingDues.Sum(p => p.Amount);
            var defaulters = pendingDues.Select(p => p.MemberId).Distinct().Count();

            var financialSummary = new FinancialHealthSummaryDto
            {
                TotalExpectedCollections = RoundingHelper.RoundToWhole(totalExpected),
                TotalActualCollections = RoundingHelper.RoundToWhole(totalCollected),
                TotalExpenses = RoundingHelper.RoundToWhole(totalExp),
                NetReserveFund = RoundingHelper.RoundToWhole(totalCollected - totalExp),
                TotalPendingDues = RoundingHelper.RoundToWhole(totalPending),
                DefaultersCount = defaulters,
                CollectionEfficiencyPercent = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0
            };

            return new ReportsSummaryDto
            {
                EventCollections = eventCollections,
                MemberContributionHistory = memberHistory,
                PendingDues = pendingDuesReport,
                EventFinancials = eventFinancials,
                PaymentModeBreakdown = paymentModeGroups,
                FinancialSummary = financialSummary
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

    public async Task<PendingDuesReportSummaryDto> GetPendingDuesReportAsync(
        int? month = null,
        int? year = null,
        Guid? eventTypeId = null,
        Guid? eventId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int selectedYear = (year.HasValue && year.Value >= 2000) ? year.Value : DateTime.UtcNow.Year;
            bool isAllMonths = (!month.HasValue || month.Value < 1 || month.Value > 12);

            List<EventSummaryDto> events;
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

                if (orgUserId.HasValue)
                {
                    events = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, null, null, cancellationToken);
                }
                else
                {
                    events = new List<EventSummaryDto>();
                }
            }
            else
            {
                events = await _eventRepository.GetAllAsync(null, null, cancellationToken);
            }

            if (eventTypeId.HasValue && eventTypeId.Value != Guid.Empty)
            {
                events = events.Where(e => e.EventTypeId == eventTypeId.Value).ToList();
            }

            if (eventId.HasValue && eventId.Value != Guid.Empty)
            {
                events = events.Where(e => e.EventId == eventId.Value).ToList();
            }

            var eventIds = events.Select(e => e.EventId).ToHashSet();
            var eventLookup = events.ToDictionary(e => e.EventId);

            var allPending = await _contributionRepository.GetPendingAsync(null, null, cancellationToken);
            allPending = allPending.Where(p => eventIds.Contains(p.EventId)).ToList();

            int selectedMonth;
            if (isAllMonths)
            {
                var pendingDatesInYear = allPending
                    .Select(p => eventLookup.TryGetValue(p.EventId, out var ev) ? ev.EventDate : (p.CreatedAt ?? DateTime.UtcNow))
                    .Where(d => d.Year == selectedYear)
                    .OrderByDescending(d => d)
                    .ToList();

                if (pendingDatesInYear.Count > 0)
                {
                    selectedMonth = pendingDatesInYear[0].Month;
                }
                else
                {
                    var allPendingDatesOverall = allPending
                        .Select(p => eventLookup.TryGetValue(p.EventId, out var ev) ? ev.EventDate : (p.CreatedAt ?? DateTime.UtcNow))
                        .OrderByDescending(d => d)
                        .ToList();

                    if (allPendingDatesOverall.Count > 0)
                    {
                        selectedMonth = allPendingDatesOverall[0].Month;
                        selectedYear = allPendingDatesOverall[0].Year;
                    }
                    else
                    {
                        selectedMonth = DateTime.UtcNow.Month;
                    }
                }
            }
            else
            {
                selectedMonth = month.GetValueOrDefault(DateTime.UtcNow.Month);
            }

            var currentMonthStart = new DateTime(selectedYear, selectedMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var prevMonthStart = currentMonthStart.AddMonths(-1);
            int prevMonth = prevMonthStart.Month;
            int prevYear = prevMonthStart.Year;

            string currentMonthLabel = currentMonthStart.ToString("MMMM");
            string previousMonthLabel = prevMonthStart.ToString("MMMM");

            List<UserDto> users = new();
            if (_userRepository != null)
            {
                try
                {
                    var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                    users = allUsers.Where(u => !u.IsDeleted).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load users for pending dues report");
                }
            }

            // Identify distinct months earlier than prevMonth that actually have pending dues
            var priorMonthsDates = allPending
                .Select(p =>
                {
                    DateTime d = eventLookup.TryGetValue(p.EventId, out var ev) ? ev.EventDate : (p.CreatedAt ?? DateTime.UtcNow);
                    return new DateTime(d.Year, d.Month, 1);
                })
                .Where(d => d < prevMonthStart)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            string priorMonthsLabel;
            if (priorMonthsDates.Count > 0)
            {
                priorMonthsLabel = string.Join(" & ", priorMonthsDates.Select(d => d.ToString("MMMM")));
            }
            else
            {
                priorMonthsLabel = "Earlier Months";
            }

            var pendingByMember = allPending.GroupBy(p => p.MemberId).ToDictionary(g => g.Key, g => g.ToList());

            var allMemberIds = new HashSet<Guid>();
            foreach (var u in users) allMemberIds.Add(u.UserId);
            foreach (var mid in pendingByMember.Keys) allMemberIds.Add(mid);

            var memberReports = new List<MemberPendingDuesReportDto>();

            foreach (var memberId in allMemberIds)
            {
                var user = users.FirstOrDefault(u => u.UserId == memberId);
                var memberPending = pendingByMember.TryGetValue(memberId, out var pList) ? pList : new List<ContributionDto>();

                string memberName = !string.IsNullOrWhiteSpace(user?.FullName)
                    ? user.FullName
                    : (memberPending.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.MemberName))?.MemberName ?? user?.Username ?? "Member");

                decimal priorAmt = 0;
                decimal prevAmt = 0;
                decimal currentAmt = 0;

                var monthlyBuckets = new Dictionary<string, (int Year, int Month, string MonthName, decimal Amount, int Count)>();

                foreach (var item in memberPending)
                {
                    var evDate = eventLookup.TryGetValue(item.EventId, out var ev) ? ev.EventDate : (item.CreatedAt ?? DateTime.UtcNow);
                    decimal roundedAmt = RoundingHelper.RoundToWhole(item.Amount);

                    if (evDate.Year == selectedYear && evDate.Month == selectedMonth)
                    {
                        currentAmt += roundedAmt;
                    }
                    else if (evDate.Year == prevYear && evDate.Month == prevMonth)
                    {
                        prevAmt += roundedAmt;
                    }
                    else if (evDate < prevMonthStart)
                    {
                        priorAmt += roundedAmt;
                    }
                    else if (isAllMonths)
                    {
                        currentAmt += roundedAmt;
                    }

                    string key = $"{evDate.Year}-{evDate.Month:D2}";
                    if (!monthlyBuckets.TryGetValue(key, out var b))
                    {
                        monthlyBuckets[key] = (evDate.Year, evDate.Month, evDate.ToString("MMMM"), roundedAmt, 1);
                    }
                    else
                    {
                        monthlyBuckets[key] = (b.Year, b.Month, b.MonthName, b.Amount + roundedAmt, b.Count + 1);
                    }
                }

                decimal totalAmt = priorAmt + prevAmt + currentAmt;

                var monthlyDetails = monthlyBuckets.Values
                    .OrderBy(b => b.Year).ThenBy(b => b.Month)
                    .Select(b => new MonthlyPendingDueDetailDto
                    {
                        Year = b.Year,
                        Month = b.Month,
                        MonthName = b.MonthName,
                        PendingAmount = b.Amount,
                        PendingEventsCount = b.Count
                    })
                    .ToList();

                memberReports.Add(new MemberPendingDuesReportDto
                {
                    MemberId = memberId,
                    MemberName = memberName,
                    Email = user?.Email ?? string.Empty,
                    Department = user?.WorkType ?? user?.RoleName ?? string.Empty,
                    WorkType = user?.WorkType ?? string.Empty,
                    PriorMonthsLabel = priorMonthsLabel,
                    PriorMonthsPendingAmount = priorAmt,
                    PreviousMonthLabel = previousMonthLabel,
                    PreviousMonthPendingAmount = prevAmt,
                    CurrentMonthLabel = currentMonthLabel,
                    CurrentMonthPendingAmount = currentAmt,
                    TotalPendingAmount = totalAmt,
                    MonthlyBreakdown = monthlyDetails
                });
            }

            memberReports = memberReports.OrderBy(m => m.MemberName).ToList();

            return new PendingDuesReportSummaryDto
            {
                SelectedMonth = isAllMonths ? 0 : selectedMonth,
                SelectedYear = selectedYear,
                CurrentMonthLabel = currentMonthLabel,
                PreviousMonthLabel = previousMonthLabel,
                PriorMonthsLabel = priorMonthsLabel,
                OverallPriorPending = memberReports.Sum(m => m.PriorMonthsPendingAmount),
                OverallPreviousPending = memberReports.Sum(m => m.PreviousMonthPendingAmount),
                OverallCurrentPending = memberReports.Sum(m => m.CurrentMonthPendingAmount),
                OverallTotalPending = memberReports.Sum(m => m.TotalPendingAmount),
                TotalDefaultersCount = memberReports.Count(m => m.TotalPendingAmount > 0),
                Members = memberReports
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("GetPendingDuesReportAsync request was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetPendingDuesReportAsync));
            throw;
        }
    }
}
