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
