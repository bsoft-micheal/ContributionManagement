using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages user login sessions, including tracking active devices and logging out remote sessions.
/// </summary>
[ApiController]
[Route(CommonRoutes.DeviceInfo.Base)]
[Authorize]
public class DeviceInfoController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public DeviceInfoController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Retrieves a list of all currently active sessions (devices) for the authenticated user.
    /// </summary>
    [HttpGet(CommonRoutes.DeviceInfo.GetActiveSessions)]
    [ActionName(nameof(GetActiveSessionAsync))]
    public async Task<ActionResult<ApiResponse<object>>> GetActiveSessionAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<object>.SuccessResult(sessions, CommonMessages.DeviceInfo.GetActiveSessionsSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the historical log of past and present logins for the authenticated user.
    /// </summary>
    [HttpGet(CommonRoutes.DeviceInfo.GetSessionHistory)]
    [ActionName(nameof(GetSessionHistoryAsync))]
    public async Task<ActionResult<ApiResponse<object>>> GetSessionHistoryAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var history = await _sessionService.GetSessionHistoryAsync(userId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<object>.SuccessResult(history, CommonMessages.DeviceInfo.GetSessionHistorySuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Terminates a specific remote session by its history ID.
    /// </summary>
    /// <param name="historyId">The unique identifier of the session history record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete(CommonRoutes.DeviceInfo.LogoutSession)]
    [ActionName(nameof(LogoutSessionAsync))]
    public async Task<ActionResult<ApiResponse>> LogoutSessionAsync(Guid historyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _sessionService.LogoutSessionAsync(historyId, userId, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.DeviceInfo.LogoutSessionSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Terminates the current active session (the device making this request).
    /// </summary>
    [HttpPost(CommonRoutes.DeviceInfo.LogoutCurrentSession)]
    [ActionName(nameof(LogoutCurrentSessionAsync))]
    public async Task<ActionResult<ApiResponse>> LogoutCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionIdClaim = User.FindFirst("SessionId")?.Value;
        if (Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            await _sessionService.LogoutSessionAsync(sessionId, userId, cancellationToken);
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.DeviceInfo.LogoutCurrentSessionSuccess, CommonStatusCodes.Status200OK));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException(CommonMessages.General.Unauthorized);
    }
}
