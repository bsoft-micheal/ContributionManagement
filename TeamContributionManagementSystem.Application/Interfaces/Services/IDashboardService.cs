using TeamContributionManagementSystem.Application.DTOs.Dashboard;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for computing metrics and statistics for the main dashboard overview.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Computes summary metrics for the dashboard, optionally filtered by month and year.
    /// </summary>
    /// <param name="month">Optional month filter (1-12).</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary DTO containing dashboard metrics and progress stats.</returns>
    Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Standardized alias method for computing dashboard summary metrics.
    /// </summary>
    /// <param name="month">Optional month filter (1-12).</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary DTO containing dashboard metrics and progress stats.</returns>
    Task<DashboardSummaryDto> GetSummaryDashboardAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);

    /// <summary>
    /// Computes KPI metrics specifically for the authenticated member dashboard.
    /// </summary>
    Task<MemberDashboardSummaryDto> GetMemberSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves event-wise contribution items with payment status for the authenticated member.
    /// </summary>
    Task<IReadOnlyCollection<MemberContributionItemDto>> GetMemberContributionsAsync(int? month = null, int? year = null, Guid? eventTypeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the authenticated member's latest payment transactions.
    /// </summary>
    Task<IReadOnlyCollection<MemberRecentPaymentDto>> GetMemberRecentPaymentsAsync(int limit = 5, CancellationToken cancellationToken = default);
}
