using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Exceptions;
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
        try
        {
            var result = await _ticketService.GetAllSupportTicketAsync(status, ticketType, priority, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<SupportTicketDto>>.SuccessResult(result, CommonMessages.SupportTickets.GetAllSuccess, CommonStatusCodes.Status200OK));
        }
        catch (Exception ex)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<IReadOnlyCollection<SupportTicketDto>>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Retrieves a support ticket by ID.
    /// </summary>
    [HttpGet(CommonRoutes.SupportTickets.GetById)]
    [ActionName(nameof(GetSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> GetSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ticketService.GetSupportTicketAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.GetByIdSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<SupportTicketDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Creates a new support ticket.
    /// </summary>
    [HttpPost(CommonRoutes.SupportTickets.Create)]
    [ActionName(nameof(SaveSupportTicketAsync))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> SaveSupportTicketAsync([FromBody] CreateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _ticketService.SaveSupportTicketAsync(request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.SaveSuccess, CommonStatusCodes.Status201Created));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(CommonStatusCodes.Status400BadRequest, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status400BadRequest));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<SupportTicketDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Updates an existing support ticket.
    /// </summary>
    [HttpPut(CommonRoutes.SupportTickets.Update)]
    [ActionName(nameof(UpdateSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> UpdateSupportTicketAsyncById(Guid id, [FromBody] UpdateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _ticketService.UpdateSupportTicketAsyncById(id, request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.UpdateSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<SupportTicketDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Adds a response or message to an existing support ticket.
    /// </summary>
    [HttpPost(CommonRoutes.SupportTickets.Reply)]
    [ActionName(nameof(ReplySupportTicketAsync))]
    public async Task<ActionResult<ApiResponse<SupportTicketDto>>> ReplySupportTicketAsync(Guid id, [FromBody] ReplyTicketRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var result = await _ticketService.ReplySupportTicketAsync(id, request, currentUser, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<SupportTicketDto>.SuccessResult(result, CommonMessages.SupportTickets.ReplySuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse<SupportTicketDto>.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<SupportTicketDto>.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse<SupportTicketDto>.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }

    /// <summary>
    /// Deletes a support ticket.
    /// </summary>
    [HttpDelete(CommonRoutes.SupportTickets.Delete)]
    [ActionName(nameof(DeleteSupportTicketAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteSupportTicketAsyncById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _ticketService.DeleteSupportTicketAsyncById(id, cancellationToken);
            return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.SupportTickets.DeleteSuccess, CommonStatusCodes.Status200OK));
        }
        catch (RecordDeletedException ex)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status404NotFound));
        }
        catch (RecordModifiedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (AlreadyProcessedException ex)
        {
            return StatusCode(CommonStatusCodes.Status409Conflict, ApiResponse.FailureResult(ex.Message, CommonStatusCodes.Status409Conflict));
        }
        catch (KeyNotFoundException)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse.FailureResult("This record no longer exists. Please refresh the page.", CommonStatusCodes.Status404NotFound));
        }
        catch (Exception)
        {
            return StatusCode(CommonStatusCodes.Status500InternalServerError, ApiResponse.FailureResult("Something went wrong. Please try again.", CommonStatusCodes.Status500InternalServerError));
        }
    }
}
