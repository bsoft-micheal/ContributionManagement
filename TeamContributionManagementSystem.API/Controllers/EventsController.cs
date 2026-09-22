using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventSummaryDto>>> GetAll([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
        => Ok(await _eventService.GetAllAsync(month, year, cancellationToken));

    /// <summary>
    /// Retrieves full details of a specific event, including all participants and their contribution status.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _eventService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Creates a new event and automatically adds the specified members as participants (Admin only).
    /// </summary>
    /// <param name="request">The event details and participant list.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventDetailsDto>> Create([FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        var eventItem = await _eventService.CreateAsync(Guid.Parse(userIdClaim), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = eventItem.EventId }, eventItem);
    }

    /// <summary>
    /// Updates an existing event and its participants (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="request">The updated event details and participant list.</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventDetailsDto>> Update(Guid id, [FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var eventItem = await _eventService.UpdateAsync(id, request, cancellationToken);
        return Ok(eventItem);
    }

    /// <summary>
    /// Deletes an event and all associated contributions/participants (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
