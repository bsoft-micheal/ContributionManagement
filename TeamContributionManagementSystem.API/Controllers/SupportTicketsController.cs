using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support-tickets")]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketService _ticketService;

    public SupportTicketsController(ISupportTicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("getAllSupportTicketAsync")]
    [ActionName("GetAllSupportTicketAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SupportTicketDto>>>> GetAllSupportTicketAsync(
        [FromQuery] string? status,
        [FromQuery] string? ticketType,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetAllSupportTicketAsync(status, ticketType, priority, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<SupportTicketDto>>.SuccessResult(result, CommonMessages.SupportTickets.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpGet("getSupportTicketAsyncById/{id:guid}")]
    [ActionName("GetSupportTicketAsyncById")]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> GetSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetSupportTicketAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpPost("saveSupportTicketAsync")]
    [ActionName("SaveSupportTicketAsync")]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> SaveSupportTicketAsync([FromBody] CreateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _ticketService.SaveSupportTicketAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    [HttpPut("updateSupportTicketAsyncById/{id:guid}")]
    [ActionName("UpdateSupportTicketAsyncById")]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> UpdateSupportTicketAsyncById(Guid id, [FromBody] UpdateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _ticketService.UpdateSupportTicketAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    [HttpPost("replySupportTicketAsync/{id:guid}")]
    [ActionName("ReplySupportTicketAsync")]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> ReplySupportTicketAsync(Guid id, [FromBody] ReplyTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _ticketService.ReplySupportTicketAsync(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.ReplySuccess, CommonStatusCodes.Status200OK));
    }

    [HttpDelete("deleteSupportTicketAsyncById/{id:guid}")]
    [ActionName("DeleteSupportTicketAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _ticketService.DeleteSupportTicketAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.SupportTickets.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
