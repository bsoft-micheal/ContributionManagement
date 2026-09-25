using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Provides aggregated statistics and summaries for the admin dashboard.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route(CommonRoutes.Dashboard.Base)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieves a high-level summary of total events, members, and financial collections.
    /// </summary>
    /// <param name="month">Optional month filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Dashboard.GetSummary)]
    [ActionName(nameof(GetSummaryDashboardAsync))]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummaryDashboardAsync([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryDashboardAsync(month, year, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<DashboardSummaryDto>.SuccessResult(result, CommonMessages.Dashboard.GetSummarySuccess, CommonStatusCodes.Status200OK));
    }
}
