using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages member financial contributions towards events.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Contributions.Base)]
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
    [HttpGet(CommonRoutes.Contributions.GetAll)]
    [ActionName(nameof(GetAllContributionAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ContributionDto>>>> GetAllContributionAsync(CancellationToken cancellationToken)
    {
        var result = await _contributionService.GetAllContributionAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<ContributionDto>>.SuccessResult(result, CommonMessages.Contributions.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves all contributions for a specific event.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Contributions.GetByEvent)]
    [ActionName(nameof(GetContributionAsyncByEvent))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ContributionDto>>>> GetContributionAsyncByEvent(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await _contributionService.GetContributionAsyncByEventId(eventId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<ContributionDto>>.SuccessResult(result, CommonMessages.Contributions.GetByEventSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a summary of the currently authenticated user's personal contributions (total paid, pending, etc.).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.Contributions.GetMySummary)]
    [ActionName(nameof(GetMySummaryAsync))]
    public async Task<ActionResult<ApiResponse<MemberContributionSummaryDto>>> GetMySummaryAsync(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(CommonConstants.Auth.EmailClaim);

        if (string.IsNullOrEmpty(email))
            return StatusCode(CommonStatusCodes.Status401Unauthorized, ApiResponse<MemberContributionSummaryDto>.FailureResult(CommonMessages.General.Unauthorized, CommonStatusCodes.Status401Unauthorized));

        var summary = await _contributionService.GetMySummaryAsync(email, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<MemberContributionSummaryDto>.SuccessResult(summary, CommonMessages.Contributions.GetMySummarySuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Processes a contribution payment for a specific event.
    /// </summary>
    /// <param name="request">The payment details (amount, event ID, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost(CommonRoutes.Contributions.Pay)]
    [ActionName(nameof(SavePayContributionAsync))]
    public async Task<ActionResult<ApiResponse<ContributionDto>>> SavePayContributionAsync([FromBody] PayContributionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _contributionService.SavePayContributionAsync(request, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<ContributionDto>.SuccessResult(result, CommonMessages.Contributions.PaySuccess, CommonStatusCodes.Status200OK));
    }
}
