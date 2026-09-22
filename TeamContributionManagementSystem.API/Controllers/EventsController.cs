using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages the creation, retrieval, and management of events (e.g., team parties) and their participants.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/events")]
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
    [HttpGet("getAllEventAsync")]
    [ActionName("GetAllEventAsync")]
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
    [HttpGet("getEventAsyncById/{id:guid}")]
    [ActionName("GetEventAsyncById")]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> GetEventAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _eventService.GetEventAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventDetailsDto>.SuccessResult(result, CommonMessages.Events.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new event and automatically adds the specified members as participants (Admin only).
    /// </summary>
    /// <param name="request">The event details and participant list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost("saveEventAsync")]
    [ActionName("SaveEventAsync")]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> SaveEventAsync([FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        var eventItem = await _eventService.SaveEventAsync(Guid.Parse(userIdClaim), request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<EventDetailsDto>.SuccessResult(eventItem, CommonMessages.Events.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing event and its participants (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The updated event details and participant list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("updateEventAsyncById/{id:guid}")]
    [ActionName("UpdateEventAsyncById")]
    public async Task<ActionResult<ApiResponse<EventDetailsDto>>> UpdateEventAsyncById(Guid id, [FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var eventItem = await _eventService.UpdateEventAsyncById(id, request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventDetailsDto>.SuccessResult(eventItem, CommonMessages.Events.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes an event and all associated contributions/participants (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("deleteEventAsyncById/{id:guid}")]
    [ActionName("DeleteEventAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteEventAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteEventAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Events.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
