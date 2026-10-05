using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages support tickets, user inquiries, and staff resolution replies.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.SupportTickets.Base)]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketService _ticketService;

    public SupportTicketsController(ISupportTicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// Retrieves a list of support tickets.
    /// </summary>
    [HttpGet(CommonRoutes.SupportTickets.GetAll)]
    [ActionName(nameof(GetAllSupportTicketAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SupportTicketDto>>>> GetAllSupportTicketAsync(
        [FromQuery] string? status,
        [FromQuery] string? ticketType,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetAllSupportTicketAsync(status, ticketType, priority, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<SupportTicketDto>>.SuccessResult(result, CommonMessages.SupportTickets.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a support ticket by ID.
    /// </summary>
    [HttpGet(CommonRoutes.SupportTickets.GetById)]
    [ActionName(nameof(GetSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> GetSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetSupportTicketAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new support ticket.
    /// </summary>
    [HttpPost(CommonRoutes.SupportTickets.Create)]
    [ActionName(nameof(SaveSupportTicketAsync))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> SaveSupportTicketAsync([FromBody] CreateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _ticketService.SaveSupportTicketAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing support ticket.
    /// </summary>
    [HttpPut(CommonRoutes.SupportTickets.Update)]
    [ActionName(nameof(UpdateSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> UpdateSupportTicketAsyncById(Guid id, [FromBody] UpdateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _ticketService.UpdateSupportTicketAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Adds a response or message to an existing support ticket.
    /// </summary>
    [HttpPost(CommonRoutes.SupportTickets.Reply)]
    [ActionName(nameof(ReplySupportTicketAsync))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> ReplySupportTicketAsync(Guid id, [FromBody] ReplyTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _ticketService.ReplySupportTicketAsync(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.ReplySuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a support ticket.
    /// </summary>
    [HttpDelete(CommonRoutes.SupportTickets.Delete)]
    [ActionName(nameof(DeleteSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _ticketService.DeleteSupportTicketAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.SupportTickets.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
