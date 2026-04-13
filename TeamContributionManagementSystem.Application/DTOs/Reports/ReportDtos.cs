namespace TeamContributionManagementSystem.Application.DTOs.Reports;

public class ReportsSummaryDto
{
    public IReadOnlyCollection<EventCollectionReportDto> EventCollections { get; set; } = Array.Empty<EventCollectionReportDto>();
    public IReadOnlyCollection<MemberContributionHistoryDto> MemberContributionHistory { get; set; } = Array.Empty<MemberContributionHistoryDto>();
    public IReadOnlyCollection<PendingDueDto> PendingDues { get; set; } = Array.Empty<PendingDueDto>();
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
}

public class MemberContributionHistoryDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalExpectedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int PaidEventsCount { get; set; }
    public int PendingEventsCount { get; set; }
}

public class PendingDueDto
{
    public Guid ContributionId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal Amount { get; set; }
}
