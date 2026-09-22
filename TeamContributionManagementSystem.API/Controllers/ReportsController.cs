using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Provides reporting functionalities for exporting and summarizing contribution data.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
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
    [HttpGet("summary")]
    public async Task<ActionResult<ReportsSummaryDto>> GetSummary([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
        => Ok(await _reportService.GetSummaryAsync(month, year, cancellationToken));
}
