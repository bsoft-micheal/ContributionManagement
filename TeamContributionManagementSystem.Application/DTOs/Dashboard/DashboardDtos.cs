namespace TeamContributionManagementSystem.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int MonthlyEventsCount { get; set; }
    public decimal TotalContributions { get; set; }
    public int PendingPayments { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public IReadOnlyCollection<UpcomingEventDto> UpcomingEvents { get; set; } = Array.Empty<UpcomingEventDto>();
}

public class UpcomingEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int PendingContributionsCount { get; set; }
    public int TotalContributionsCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
}
