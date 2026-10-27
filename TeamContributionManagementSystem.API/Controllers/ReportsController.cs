using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Provides reporting functionalities for exporting and summarizing contribution data.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Reports.Base)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Generates a summary report of contributions, optionally filtered by month and year.
    /// </summary>
    /// <param name="month">Optional month filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Reports.GetSummary)]
    [ActionName(nameof(GetSummaryReportAsync))]
    public async Task<ActionResult<ApiResponse<ReportsSummaryDto>>> GetSummaryReportAsync([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _reportService.GetSummaryReportAsync(month, year, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ReportsSummaryDto>.SuccessResult(result, CommonMessages.Reports.GetSummarySuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Generates a pending dues report detailing prior months, previous month, current month, and total pending per member.
    /// </summary>
    /// <param name="month">Optional month filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="eventTypeId">Optional event type filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Reports.GetPendingDues)]
    [ActionName(nameof(GetPendingDuesReportAsync))]
    public async Task<ActionResult<ApiResponse<PendingDuesReportSummaryDto>>> GetPendingDuesReportAsync(
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] Guid? eventTypeId,
        [FromQuery] Guid? eventId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPendingDuesReportAsync(month, year, eventTypeId, eventId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PendingDuesReportSummaryDto>.SuccessResult(result, "Pending dues report generated successfully.", CommonStatusCodes.Status200OK));
    }
}
