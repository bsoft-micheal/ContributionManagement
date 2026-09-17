using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventTypeDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _eventTypeService.GetAllAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventTypeDto>> Create([FromBody] CreateEventTypeRequestDto request, CancellationToken cancellationToken)
        => Ok(await _eventTypeService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EventTypeDto>> Update(Guid id, [FromBody] UpdateEventTypeRequestDto request, CancellationToken cancellationToken)
        => Ok(await _eventTypeService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _eventTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
