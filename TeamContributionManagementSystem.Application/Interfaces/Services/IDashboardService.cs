using TeamContributionManagementSystem.Application.DTOs.Dashboard;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<DashboardSummaryDto> GetSummaryDashboardAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);
}
