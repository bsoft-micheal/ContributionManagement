using TeamContributionManagementSystem.Application.DTOs.Reports;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IReportService
{
    Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<ReportsSummaryDto> GetSummaryReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);
}
