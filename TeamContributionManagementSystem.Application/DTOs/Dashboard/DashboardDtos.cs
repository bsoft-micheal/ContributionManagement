namespace TeamContributionManagementSystem.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int MonthlyEventsCount { get; set; }
    public decimal TotalContributions { get; set; }
    public int PendingPayments { get; set; }
    public IReadOnlyCollection<UpcomingEventDto> UpcomingEvents { get; set; } = Array.Empty<UpcomingEventDto>();
}

public class UpcomingEventDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ExpectedAmount { get; set; }
}
