using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
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
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService? _currentUserService;

    public DashboardController(
        IDashboardService dashboardService,
        IEventRepository eventRepository,
        ICurrentUserService? currentUserService = null)
    {
        _dashboardService = dashboardService;
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
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

    /// <summary>
    /// Retrieves dashboard events directly using EventRepository, scoped to member or all events for admin/organizer.
    /// </summary>
    /// <param name="month">Optional month filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Dashboard.GetEvents)]
    [ActionName(nameof(GetEventsDashboardAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventSummaryDto>>>> GetEventsDashboardAsync([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        int? filterMonth = (month == 0 || month == null) ? null : month;
        int? filterYear = (year == 0 || year == null) ? null : year;

        if (_currentUserService != null && _currentUserService.IsMemberRole)
        {
            var myMemberId = _currentUserService.MemberId;
            if (myMemberId.HasValue)
            {
                var memberEvents = await _eventRepository.GetAllForMemberAsync(myMemberId.Value, filterMonth, filterYear, cancellationToken);
                return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventSummaryDto>>.SuccessResult(memberEvents, CommonMessages.Events.GetAllSuccess, CommonStatusCodes.Status200OK));
            }
        }

        var allEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventSummaryDto>>.SuccessResult(allEvents, CommonMessages.Events.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves dynamic financial and event summary metrics for the current calendar month.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Dashboard.GetCurrentMonthSummary)]
    [ActionName(nameof(GetCurrentMonthSummaryAsync))]
    public async Task<ActionResult<ApiResponse<CurrentMonthSummaryDto>>> GetCurrentMonthSummaryAsync(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetCurrentMonthSummaryAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<CurrentMonthSummaryDto>.SuccessResult(result, CommonMessages.Dashboard.GetCurrentMonthSummarySuccess, CommonStatusCodes.Status200OK));
    }
}
