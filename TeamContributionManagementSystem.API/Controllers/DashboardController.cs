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
[Route("api/dashboard")]
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

        if (_currentUserService != null && _currentUserService.IsOrganizerRole)
        {
            Guid? orgUserId = null;
            if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
            {
                orgUserId = parsedGuid;
            }
            else if (_currentUserService.MemberId.HasValue)
            {
                orgUserId = _currentUserService.MemberId.Value;
            }

            if (orgUserId.HasValue)
            {
                var orgEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, filterMonth, filterYear, cancellationToken);
                return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventSummaryDto>>.SuccessResult(orgEvents, CommonMessages.Events.GetAllSuccess, CommonStatusCodes.Status200OK));
            }
        }

        var allEvents = await _eventRepository.GetAllAsync(filterMonth, filterYear, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventSummaryDto>>.SuccessResult(allEvents, CommonMessages.Events.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves KPI summary metrics for the authenticated member dashboard.
    /// </summary>
    [HttpGet("member/summary")]
    [HttpGet("getMemberSummaryAsync")]
    [ActionName(nameof(GetMemberSummaryAsync))]
    public async Task<ActionResult<ApiResponse<MemberDashboardSummaryDto>>> GetMemberSummaryAsync([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetMemberSummaryAsync(month, year, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<MemberDashboardSummaryDto>.SuccessResult(result, "Member dashboard summary retrieved successfully.", CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves event-wise contribution items with payment eligibility for the authenticated member.
    /// </summary>
    [HttpGet("member/contributions")]
    [HttpGet("getMemberContributionsAsync")]
    [ActionName(nameof(GetMemberContributionsAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MemberContributionItemDto>>>> GetMemberContributionsAsync([FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? eventTypeId, CancellationToken cancellationToken)
    {
        Guid? parsedGuid = null;
        string? eventTypeName = null;

        if (!string.IsNullOrWhiteSpace(eventTypeId) && !string.Equals(eventTypeId, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(eventTypeId, out var g))
            {
                parsedGuid = g;
            }
            else
            {
                eventTypeName = eventTypeId.Trim();
            }
        }

        var result = await _dashboardService.GetMemberContributionsAsync(month, year, parsedGuid, cancellationToken);

        if (!string.IsNullOrWhiteSpace(eventTypeName))
        {
            result = result
                .Where(x => string.Equals(x.EventTypeName?.Trim(), eventTypeName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<MemberContributionItemDto>>.SuccessResult(result, "Member contributions retrieved successfully.", CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the authenticated member's latest five payment transactions.
    /// </summary>
    [HttpGet("member/recent-payments")]
    [HttpGet("getMemberRecentPaymentsAsync")]
    [ActionName(nameof(GetMemberRecentPaymentsAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MemberRecentPaymentDto>>>> GetMemberRecentPaymentsAsync([FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetMemberRecentPaymentsAsync(limit, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<MemberRecentPaymentDto>>.SuccessResult(result, "Member recent payments retrieved successfully.", CommonStatusCodes.Status200OK));
    }
}
