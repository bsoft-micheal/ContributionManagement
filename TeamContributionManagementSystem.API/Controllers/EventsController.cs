using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.API.Attributes;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages the creation, retrieval, and management of events (e.g., team parties) and their participants.
/// </summary>
[ApiController]
[Authorize]
[RequireFeaturePermission(4, AccessType.ReadOnly)]
[ApiVersion("1.0")]
[Route(CommonRoutes.Events.Base)]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Retrieves a high-level list of all events, optionally filtered by month and year.
    /// </summary>
    /// <param name="month">Optional month filter (1-12).</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Events.GetAll)]
    [ActionName(nameof(GetAllEventAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventSummaryDto>>>> GetAllEventAsync([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetAllEventAsync(month, year, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventSummaryDto>>.SuccessResult(result, CommonMessages.Events.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves full details of a specific event, including all participants and their contribution status.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Events.GetById)]
    [ActionName(nameof(GetEventAsyncById))]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> GetEventAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventDetailsDto>.SuccessResult(result, CommonMessages.Events.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves monthly event restrictions, including taken event types and already-celebrating members for a given month and year.
    /// </summary>
    [HttpGet("getMonthlyRestrictionsAsync")]
    [ActionName(nameof(GetMonthlyRestrictionsAsync))]
    public async Task<ActionResult<ApiResponse<MonthlyEventRestrictionsDto>>> GetMonthlyRestrictionsAsync(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] Guid? excludeEventId,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.GetMonthlyRestrictionsAsync(month, year, excludeEventId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<MonthlyEventRestrictionsDto>.SuccessResult(result, "Monthly event restrictions retrieved successfully.", CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a list of all active users who are eligible to be assigned as event sponsors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Events.GetEligibleSponsors)]
    [HttpGet(CommonRoutes.Events.GetEligibleSponsorsAsync)]
    [ActionName(nameof(GetEligibleSponsorsAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EligibleSponsorDto>>>> GetEligibleSponsorsAsync(CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEligibleSponsorsAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EligibleSponsorDto>>.SuccessResult(result, "Eligible sponsors retrieved successfully.", CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new event and automatically adds the specified members as participants (Admin or Organizer).
    /// </summary>
    /// <param name="request">The event details and participant list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [RequireFeaturePermission(31, AccessType.ReadWrite)]
    [HttpPost(CommonRoutes.Events.Create)]
    [ActionName(nameof(SaveEventAsync))]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> SaveEventAsync([FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(CommonMessages.General.UserIdentityNotAvailable);

        var eventItem = await _eventService.SaveEventAsync(Guid.Parse(userIdClaim), request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<EventDetailsDto>.SuccessResult(eventItem, CommonMessages.Events.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing event and its participants (Admin or Organizer).
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The updated event details and participant list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [RequireFeaturePermission(32, AccessType.ReadWrite)]
    [HttpPut(CommonRoutes.Events.Update)]
    [ActionName(nameof(UpdateEventAsyncById))]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> UpdateEventAsyncById(Guid id, [FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var eventItem = await _eventService.UpdateEventAsyncById(id, request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventDetailsDto>.SuccessResult(eventItem, CommonMessages.Events.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes an event and all associated contributions/participants (Admin or Organizer).
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [RequireFeaturePermission(33, AccessType.ReadWrite)]
    [HttpDelete(CommonRoutes.Events.Delete)]
    [ActionName(nameof(DeleteEventAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteEventAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteEventAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Events.DeleteSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Publishes a draft event, making it visible to members and dispatching notifications (Admin or Organizer).
    /// </summary>
    /// <param name="id">The unique identifier of the event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [RequireFeaturePermission(32, AccessType.ReadWrite)]
    [HttpPost(CommonRoutes.Events.Publish)]
    [HttpPost(CommonRoutes.Events.PublishAsync)]
    [ActionName(nameof(PublishEventAsyncById))]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> PublishEventAsyncById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(CommonMessages.General.UserIdentityNotAvailable);

        var eventItem = await _eventService.PublishEventAsyncById(id, Guid.Parse(userIdClaim), cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventDetailsDto>.SuccessResult(
            eventItem,
            "Event published successfully. Member notifications are being processed.",
            CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Manually triggers contribution reminder emails for all pending members of an event (Admin or Organizer).
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.AdminOrOrganizer)]
    [HttpPost(CommonRoutes.Events.SendReminders)]
    [HttpPost(CommonRoutes.Events.SendRemindersAsync)]
    [ActionName(nameof(SendRemindersAsync))]
    public ActionResult<ApiResponse<object>> SendRemindersAsync([FromRoute] Guid eventId, CancellationToken cancellationToken)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name) 
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? "Admin";

        var jobId = BackgroundJob.Enqueue<IContributionReminderJobService>(
            service => service.SendRemindersForEventAsync(eventId, userName, CancellationToken.None));

        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<object>.SuccessResult(
            new { JobId = jobId, EventId = eventId, Status = "Enqueued" },
            "Event contribution reminder job successfully enqueued.",
            CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Previews individual member contributions based on service years tenure rules and event amount before event creation.
    /// </summary>
    /// <param name="request">Preview parameters including event type, date, amount, and participants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost(CommonRoutes.Events.PreviewContributions)]
    [HttpPost("contribution-preview")]
    [ActionName(nameof(PreviewContributionsAsync))]
    public async Task<ActionResult<ApiResponse<EventContributionPreviewResponseDto>>> PreviewContributionsAsync(
        [FromBody] EventContributionPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.PreviewContributionAsync(request, cancellationToken);
        return StatusCode(
            CommonStatusCodes.Status200OK,
            ApiResponse<EventContributionPreviewResponseDto>.SuccessResult(
                result,
                CommonMessages.Events.PreviewSuccess,
                CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the current available Amount In Hand (liquidity) and unspent allocations for new events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Events.GetAvailableAmountInHand)]
    [HttpGet(CommonRoutes.Events.GetAvailableBalance)]
    [ActionName(nameof(GetAvailableAmountInHandAsync))]
    public async Task<ActionResult<ApiResponse<AvailableAmountInHandDto>>> GetAvailableAmountInHandAsync(
        [FromQuery] Guid? excludeEventId,
        CancellationToken cancellationToken)
    {
        var result = await _eventService.GetAvailableAmountInHandAsync(excludeEventId, cancellationToken);
        return StatusCode(
            CommonStatusCodes.Status200OK,
            ApiResponse<AvailableAmountInHandDto>.SuccessResult(
                result,
                "Available Amount In Hand retrieved successfully.",
                CommonStatusCodes.Status200OK));
    }
}
