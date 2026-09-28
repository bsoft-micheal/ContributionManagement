using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Reports;
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

    public ReportService(
        ILogger<ReportService> logger, 
        IEventRepository eventRepository, 
        IContributionRepository contributionRepository,
        IExpenseRepository expenseRepository,
        IPaymentTransactionRepository paymentTransactionRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _contributionRepository = contributionRepository;
        _expenseRepository = expenseRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
    }

    public async Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int? targetMonth = (month == 0 || month == null) ? null : month;
            int? targetYear = (year == 0 || year == null) ? null : year;

            var events = await _eventRepository.GetAllAsync(targetMonth, targetYear, cancellationToken);
            var pendingDues = await _contributionRepository.GetPendingAsync(targetMonth, targetYear, cancellationToken);
            var contributions = await _contributionRepository.GetAllAsync(cancellationToken);

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

            var allExpenses = await _expenseRepository.GetAllAsync(startDate: startDate, endDate: endDate, cancellationToken: cancellationToken);
            var allPayments = await _paymentTransactionRepository.GetAllAsync(startDate: startDate, endDate: endDate, cancellationToken: cancellationToken);

            // 1. Event Collections Report
            var eventCollections = events.Select(x =>
            {
                var expected = x.TotalExpectedAmount;
                var paid = x.TotalPaidAmount;
                var pending = expected - paid;
                var rate = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;

                return new EventCollectionReportDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventTypeName,
                    EventDate = x.EventDate,
                    ExpectedAmount = expected,
                    PaidAmount = paid,
                    PendingAmount = pending,
                    CollectionRate = rate,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt
                };
            }).OrderByDescending(x => x.EventDate).ToList();

            // 2. Member Contributions History Report
            var memberHistory = contributions
                .GroupBy(x => new { x.MemberId, MemberName = x.MemberName })
                .Select(group =>
                {
                    var expected = group.Sum(x => x.Amount);
                    var paid = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount);
                    var completion = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;
                    var firstItem = group.FirstOrDefault();

                    return new MemberContributionHistoryDto
                    {
                        MemberId = group.Key.MemberId,
                        MemberName = group.Key.MemberName,
                        TotalExpectedAmount = expected,
                        TotalPaidAmount = paid,
                        PaidEventsCount = group.Count(x => x.PaymentStatus == PaymentStatus.Paid),
                        PendingEventsCount = group.Count(x => x.PaymentStatus != PaymentStatus.Paid),
                        CompletionRate = completion,
                        CreatedBy = firstItem?.CreatedBy,
                        CreatedAt = firstItem?.CreatedAt
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

                return new PendingDueDto
                {
                    ContributionId = x.ContributionId,
                    MemberId = x.MemberId,
                    MemberName = x.MemberName,
                    Phone = string.Empty,
                    EventName = x.EventName,
                    EventDate = DateTime.MinValue,
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
                    TotalCollections = collections,
                    TotalExpenses = expenses,
                    NetBalance = net,
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
                    TotalAmount = kvp.Value.TotalAmount,
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
                        TotalAmount = g.Sum(p => p.Amount),
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
                TotalExpectedCollections = totalExpected,
                TotalActualCollections = totalCollected,
                TotalExpenses = totalExp,
                NetReserveFund = totalCollected - totalExp,
                TotalPendingDues = totalPending,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetSummaryAsync));
            throw;
        }
    }
}
