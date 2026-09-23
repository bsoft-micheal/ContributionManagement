using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly Microsoft.Extensions.Logging.ILogger<ReportService> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IPaymentTransactionRepository _paymentTransactionRepository;

    public ReportService(
        Microsoft.Extensions.Logging.ILogger<ReportService> logger, 
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
            var contributions = events.SelectMany(x => x.Contributions.Where(c => !c.IsDeleted)).ToList();

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
                var expected = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount);
                var paid = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount);
                var pending = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus != PaymentStatus.Paid).Sum(c => c.Amount);
                var rate = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;

                return new EventCollectionReportDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventType?.EventTypeName ?? string.Empty,
                    EventDate = x.EventDate,
                    ExpectedAmount = expected,
                    PaidAmount = paid,
                    PendingAmount = pending,
                    CollectionRate = rate
                };
            }).OrderByDescending(x => x.EventDate).ToList();

            // 2. Member Contributions History Report
            var memberHistory = contributions
                .GroupBy(x => new { x.MemberId, MemberName = x.Member?.Name ?? string.Empty })
                .Select(group =>
                {
                    var expected = group.Sum(x => x.Amount);
                    var paid = group.Where(x => x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount);
                    var completion = expected > 0 ? Math.Round((paid / expected) * 100, 1) : 0;

                    return new MemberContributionHistoryDto
                    {
                        MemberId = group.Key.MemberId,
                        MemberName = group.Key.MemberName,
                        TotalExpectedAmount = expected,
                        TotalPaidAmount = paid,
                        PaidEventsCount = group.Count(x => x.PaymentStatus == PaymentStatus.Paid),
                        PendingEventsCount = group.Count(x => x.PaymentStatus != PaymentStatus.Paid),
                        CompletionRate = completion
                    };
                })
                .OrderBy(x => x.MemberName)
                .ToList();

            // 3. Pending Dues & Defaulters Report
            var pendingDuesReport = pendingDues.Select(x =>
            {
                var days = x.Event?.EventDate != null 
                    ? Math.Max(0, (DateTime.UtcNow - x.Event.EventDate).Days)
                    : 0;
                var aging = days > 30 ? "Critical (> 30d)" : (days >= 15 ? "Moderate (15-30d)" : "Recent (< 15d)");

                return new PendingDueDto
                {
                    ContributionId = x.ContributionId,
                    MemberId = x.MemberId,
                    MemberName = x.Member?.Name ?? string.Empty,
                    Phone = x.Member?.Phone ?? string.Empty,
                    EventName = x.Event?.EventName ?? string.Empty,
                    EventDate = x.Event?.EventDate ?? DateTime.MinValue,
                    Amount = x.Amount,
                    DaysOverdue = days,
                    AgingCategory = aging
                };
            }).OrderByDescending(x => x.DaysOverdue).ToList();

            // 4. Event Financials (Budget vs Actual Expenses)
            var eventFinancials = events.Select(x =>
            {
                var collections = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount);
                var expenses = allExpenses
                    .Where(e => !e.IsDeleted && !string.IsNullOrWhiteSpace(e.EventName) && e.EventName.Trim().Equals(x.EventName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(e => e.Amount);
                var net = collections - expenses;
                var savingsRate = collections > 0 ? Math.Round((net / collections) * 100, 1) : 0;

                return new EventFinancialReportDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeName = x.EventType?.EventTypeName ?? string.Empty,
                    EventDate = x.EventDate,
                    TotalCollections = collections,
                    TotalExpenses = expenses,
                    NetBalance = net,
                    Status = net >= 0 ? "Surplus" : "Deficit",
                    SavingsRatePercent = savingsRate
                };
            }).OrderByDescending(x => x.EventDate).ToList();

            // 5. Payment Mode Breakdown
            var paidContributions = contributions.Where(c => c.PaymentStatus == PaymentStatus.Paid).ToList();
            var totalPaidFromContributions = paidContributions.Sum(c => c.Amount);

            var paymentModeGroups = paidContributions
                .GroupBy(c => c.PaymentMode == PaymentMode.None ? "UPI" : c.PaymentMode.ToString())
                .Select(g => new PaymentModeReportDto
                {
                    PaymentMode = g.Key,
                    TransactionCount = g.Count(),
                    TotalAmount = g.Sum(c => c.Amount),
                    Percentage = totalPaidFromContributions > 0 ? Math.Round((g.Sum(c => c.Amount) / totalPaidFromContributions) * 100, 1) : 0,
                    VerifiedCount = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            if (!paymentModeGroups.Any() && allPayments.Any())
            {
                var totalFromPayments = allPayments.Sum(p => p.Amount);
                paymentModeGroups = allPayments
                    .GroupBy(p => string.IsNullOrWhiteSpace(p.PaymentMode) ? "UPI" : p.PaymentMode)
                    .Select(g => new PaymentModeReportDto
                    {
                        PaymentMode = g.Key,
                        TransactionCount = g.Count(),
                        TotalAmount = g.Sum(p => p.Amount),
                        Percentage = totalFromPayments > 0 ? Math.Round((g.Sum(p => p.Amount) / totalFromPayments) * 100, 1) : 0,
                        VerifiedCount = g.Count(p => p.Status == "Verified" || p.Status == "Approved")
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();
            }

            // High-Level Financial Summary KPIs
            var totalExpected = contributions.Sum(c => c.Amount);
            var totalCollected = paidContributions.Sum(c => c.Amount);
            var totalExp = allExpenses.Where(e => !e.IsDeleted).Sum(e => e.Amount);
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
            _logger.LogError(ex, "Error in GetSummaryAsync");
            throw;
        }
    }
}
