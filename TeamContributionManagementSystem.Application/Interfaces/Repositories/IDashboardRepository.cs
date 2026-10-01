namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<(int TotalCount, decimal TotalCollected, decimal TotalExpensed)> GetCurrentMonthMetricsAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}
