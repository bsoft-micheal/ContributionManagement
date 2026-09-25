using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.TicketTypes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Support Data Ticket Types.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ticket-types")]
public class TicketTypesController : ControllerBase
{
    private readonly ITicketTypeService _ticketTypeService;

    public TicketTypesController(ITicketTypeService ticketTypeService)
    {
        _ticketTypeService = ticketTypeService;
    }

    /// <summary>
    /// Retrieves all ticket types.
    /// </summary>
    [HttpGet("getAllTicketTypeAsync")]
    [ActionName("GetAllTicketTypeAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TicketTypeDto>>>> GetAllTicketTypeAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var result = await _ticketTypeService.GetAllTicketTypeAsync(activeOnly, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<TicketTypeDto>>.SuccessResult(result, CommonMessages.TicketTypes.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a ticket type by ID.
    /// </summary>
    [HttpGet("getTicketTypeAsyncById/{id:guid}")]
    [ActionName("GetTicketTypeAsyncById")]
    public async Task<ActionResult<ApiResponse<TicketTypeDto>>> GetTicketTypeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ticketTypeService.GetTicketTypeAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<TicketTypeDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<TicketTypeDto>.SuccessResult(result, CommonMessages.TicketTypes.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new ticket type (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("saveTicketTypeAsync")]
    [ActionName("SaveTicketTypeAsync")]
    public async Task<ActionResult<ApiResponse<TicketTypeDto>>> SaveTicketTypeAsync([FromBody] CreateTicketTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _ticketTypeService.SaveTicketTypeAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<TicketTypeDto>.SuccessResult(result, CommonMessages.TicketTypes.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing ticket type (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("updateTicketTypeAsyncById/{id:guid}")]
    [ActionName("UpdateTicketTypeAsyncById")]
    public async Task<ActionResult<ApiResponse<TicketTypeDto>>> UpdateTicketTypeAsyncById(Guid id, [FromBody] UpdateTicketTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _ticketTypeService.UpdateTicketTypeAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<TicketTypeDto>.SuccessResult(result, CommonMessages.TicketTypes.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a ticket type (Admin only).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("deleteTicketTypeAsyncById/{id:guid}")]
    [ActionName("DeleteTicketTypeAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteTicketTypeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _ticketTypeService.DeleteTicketTypeAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.TicketTypes.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
