namespace TeamContributionManagementSystem.Application.DTOs.Reports;

public class ReportsSummaryDto
{
    public IReadOnlyCollection<EventCollectionReportDto> EventCollections { get; set; } = Array.Empty<EventCollectionReportDto>();
    public IReadOnlyCollection<MemberContributionHistoryDto> MemberContributionHistory { get; set; } = Array.Empty<MemberContributionHistoryDto>();
    public IReadOnlyCollection<PendingDueDto> PendingDues { get; set; } = Array.Empty<PendingDueDto>();
    public IReadOnlyCollection<EventFinancialReportDto> EventFinancials { get; set; } = Array.Empty<EventFinancialReportDto>();
    public IReadOnlyCollection<PaymentModeReportDto> PaymentModeBreakdown { get; set; } = Array.Empty<PaymentModeReportDto>();
    public FinancialHealthSummaryDto FinancialSummary { get; set; } = new();
}

public class EventCollectionReportDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CollectionRate { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal RemainingAmount => ExpectedAmount - ExpenseAmount;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
}

public class MemberContributionHistoryDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalExpectedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int PaidEventsCount { get; set; }
    public int PendingEventsCount { get; set; }
    public decimal CompletionRate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
    public IReadOnlyCollection<MemberEventDetailDto> Events { get; set; } = Array.Empty<MemberEventDetailDto>();
}

public class MemberEventDetailDto
{
    public Guid ContributionId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMode { get; set; }
}

public class PendingDueDto
{
    public Guid ContributionId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal Amount { get; set; }
    public int DaysOverdue { get; set; }
    public string AgingCategory { get; set; } = "Recent";
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
}

public class EventFinancialReportDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal TotalCollections { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetBalance { get; set; }
    public string Status { get; set; } = "Surplus"; // "Surplus" or "Deficit"
    public decimal SavingsRatePercent { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
}

public class PaymentModeReportDto
{
    public string PaymentMode { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
    public int VerifiedCount { get; set; }
}

public class FinancialHealthSummaryDto
{
    public decimal TotalExpectedCollections { get; set; }
    public decimal TotalActualCollections { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetReserveFund { get; set; }
    public decimal TotalPendingDues { get; set; }
    public int DefaultersCount { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
}

public class MonthlyPendingDueDetailDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal PendingAmount { get; set; }
    public int PendingEventsCount { get; set; }
}

public class MemberPendingDuesReportDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;

    public string PriorMonthsLabel { get; set; } = string.Empty;
    public decimal PriorMonthsPendingAmount { get; set; }

    public string PreviousMonthLabel { get; set; } = string.Empty;
    public decimal PreviousMonthPendingAmount { get; set; }

    public string CurrentMonthLabel { get; set; } = string.Empty;
    public decimal CurrentMonthPendingAmount { get; set; }

    public decimal TotalPendingAmount { get; set; }

    public IReadOnlyCollection<MonthlyPendingDueDetailDto> MonthlyBreakdown { get; set; } = Array.Empty<MonthlyPendingDueDetailDto>();
}

public class PendingDuesReportSummaryDto
{
    public int SelectedMonth { get; set; }
    public int SelectedYear { get; set; }
    public string CurrentMonthLabel { get; set; } = string.Empty;
    public string PreviousMonthLabel { get; set; } = string.Empty;
    public string PriorMonthsLabel { get; set; } = string.Empty;

    public decimal OverallPriorPending { get; set; }
    public decimal OverallPreviousPending { get; set; }
    public decimal OverallCurrentPending { get; set; }
    public decimal OverallTotalPending { get; set; }
    public int TotalDefaultersCount { get; set; }

    public IReadOnlyCollection<string> DynamicMonthColumns { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<MemberPendingDuesReportDto> Members { get; set; } = Array.Empty<MemberPendingDuesReportDto>();
}

