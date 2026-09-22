using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SupportTicketDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? ticketType,
        [FromQuery] string? priority,
        CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetAllAsync(status, ticketType, priority, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ticketService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupportTicketDto>> Create([FromBody] CreateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _ticketService.CreateAsync(request, currentUser, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.TicketId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupportTicketDto>> Update(Guid id, [FromBody] UpdateSupportTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _ticketService.UpdateAsync(id, request, currentUser, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<ActionResult<SupportTicketDto>> Reply(Guid id, [FromBody] ReplyTicketRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Admin";
        var result = await _ticketService.ReplyAsync(id, request, currentUser, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _ticketService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
