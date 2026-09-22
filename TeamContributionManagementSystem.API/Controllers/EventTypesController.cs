using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages the types of events (e.g., Birthday, Farewell) and their default properties.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/event-types")]
public class EventTypesController : ControllerBase
{
    private readonly IEventTypeService _eventTypeService;

    public EventTypesController(IEventTypeService eventTypeService)
    {
        _eventTypeService = eventTypeService;
    }

    /// <summary>
    /// Retrieves a list of all configured event types.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventTypeDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _eventTypeService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Creates a new event type (Admin only).
    /// </summary>
    /// <param name="request">The event type details.</param>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventTypeDto>> Create([FromBody] CreateEventTypeRequestDto request, CancellationToken cancellationToken)
        => Ok(await _eventTypeService.CreateAsync(request, cancellationToken));

    /// <summary>
    /// Updates an existing event type (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event type.</param>
    /// <param name="request">The updated event type details.</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventTypeDto>> Update(Guid id, [FromBody] UpdateEventTypeRequestDto request, CancellationToken cancellationToken)
        => Ok(await _eventTypeService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Deletes an event type (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event type to delete.</param>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _eventTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
