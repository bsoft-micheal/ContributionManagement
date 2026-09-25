using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages the types of events (e.g., Birthday, Farewell) and their default properties.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.EventTypes.Base)]
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
    [HttpGet(CommonRoutes.EventTypes.GetAll)]
    [ActionName(nameof(GetAllEventTypeAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<EventTypeDto>>>> GetAllEventTypeAsync(CancellationToken cancellationToken)
    {
        var result = await _eventTypeService.GetAllEventTypeAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<EventTypeDto>>.SuccessResult(result, CommonMessages.EventTypes.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new event type (Admin only).
    /// </summary>
    /// <param name="request">The event type details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.EventTypes.Create)]
    [ActionName(nameof(SaveEventTypeAsync))]
    public async Task<ActionResult<ApiResponse<EventTypeDto>>> SaveEventTypeAsync([FromBody] CreateEventTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _eventTypeService.SaveEventTypeAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<EventTypeDto>.SuccessResult(result, CommonMessages.EventTypes.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing event type (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event type.</param>
    /// <param name="request">The updated event type details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.EventTypes.Update)]
    [ActionName(nameof(UpdateEventTypeAsyncById))]
    public async Task<ActionResult<ApiResponse<EventTypeDto>>> UpdateEventTypeAsyncById(Guid id, [FromBody] UpdateEventTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _eventTypeService.UpdateEventTypeAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<EventTypeDto>.SuccessResult(result, CommonMessages.EventTypes.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes an event type (Admin only).
    /// </summary>
    /// <param name="id">The unique identifier of the event type to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.EventTypes.Delete)]
    [ActionName(nameof(DeleteEventTypeAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteEventTypeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _eventTypeService.DeleteEventTypeAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.EventTypes.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
