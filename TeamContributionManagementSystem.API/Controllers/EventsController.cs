using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventSummaryDto>>> GetAll([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
        => Ok(await _eventService.GetAllAsync(month, year, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _eventService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventDetailsDto>> Create([FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity is not available.");

        var eventItem = await _eventService.CreateAsync(Guid.Parse(userIdClaim), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = eventItem.EventId }, eventItem);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventDetailsDto>> Update(Guid id, [FromBody] CreateEventRequestDto request, CancellationToken cancellationToken)
    {
        var eventItem = await _eventService.UpdateAsync(id, request, cancellationToken);
        return Ok(eventItem);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
