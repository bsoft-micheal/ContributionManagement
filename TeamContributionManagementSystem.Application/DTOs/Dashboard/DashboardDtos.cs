namespace TeamContributionManagementSystem.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    private decimal? _totalExpectedAmount;
    private decimal? _totalRemainingAmount;

    public int MonthlyEventsCount { get; set; }
    public decimal TotalContributions { get; set; }
    public int PendingPayments { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalExpectedAmount
    {
        get => _totalExpectedAmount ?? (TotalContributions + TotalPendingAmount);
        set => _totalExpectedAmount = value;
    }
    public decimal TotalRemainingAmount
    {
        get => _totalRemainingAmount ?? (TotalExpectedAmount - TotalExpenses);
        set => _totalRemainingAmount = value;
    }
    public IReadOnlyCollection<UpcomingEventDto> UpcomingEvents { get; set; } = Array.Empty<UpcomingEventDto>();
}

public class UpcomingEventDto
{
    private decimal? _remainingAmount;

    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal RemainingAmount
    {
        get => _remainingAmount ?? (ExpectedAmount - ExpenseAmount);
        set => _remainingAmount = value;
    }
    public decimal OverallExpectedAmount { get; set; }
    public decimal OverallExpenseAmount { get; set; }
    public decimal OverallRemainingAmount { get; set; }
    public int PendingContributionsCount { get; set; }
    public int TotalContributionsCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn => CreatedAt;
}
