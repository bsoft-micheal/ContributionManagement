using TeamContributionManagementSystem.Application.DTOs.Reports;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for generating contribution summary reports and analytics.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a summary report of contributions, optionally filtered by month and year.
    /// </summary>
    /// <param name="month">Optional month filter (1-12).</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task returning the calculated summary statistics and metrics.</returns>
    Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Standardized alias method for generating a summary report of contributions.
    /// </summary>
    /// <param name="month">Optional month filter (1-12).</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task returning the calculated summary statistics and metrics.</returns>
    Task<ReportsSummaryDto> GetSummaryReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);
}
