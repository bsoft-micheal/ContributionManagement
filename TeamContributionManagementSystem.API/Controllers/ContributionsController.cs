using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages member financial contributions towards events.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/contributions")]
public class ContributionsController : ControllerBase
{
    private readonly IContributionService _contributionService;

    public ContributionsController(IContributionService contributionService)
    {
        _contributionService = contributionService;
    }

    /// <summary>
    /// Retrieves a list of all contributions across all events.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ContributionDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _contributionService.GetAllAsync(cancellationToken));

    /// <summary>
    /// Retrieves all contributions for a specific event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    [HttpGet("event/{eventId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<ContributionDto>>> GetByEvent(Guid eventId, CancellationToken cancellationToken)
        => Ok(await _contributionService.GetByEventIdAsync(eventId, cancellationToken));

    /// <summary>
    /// Retrieves a summary of the currently authenticated user's personal contributions (total paid, pending, etc.).
    /// </summary>
    [HttpGet("my-summary")]
    public async Task<ActionResult<MemberContributionSummaryDto>> GetMySummary(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("email");

        if (string.IsNullOrEmpty(email))
            return Unauthorized(new { message = "Unable to determine current user identity." });

        var summary = await _contributionService.GetMySummaryAsync(email, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Processes a contribution payment for a specific event.
    /// </summary>
    /// <param name="request">The payment details (amount, event ID, etc.).</param>
    [HttpPost("pay")]
    public async Task<ActionResult<ContributionDto>> Pay([FromBody] PayContributionRequestDto request, CancellationToken cancellationToken)
        => Ok(await _contributionService.PayAsync(request, cancellationToken));
}
