using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages user login sessions, including tracking active devices and logging out remote sessions.
/// </summary>
[ApiController]
[Route("api/v1/device-info")]
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
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);
        return Ok(sessions);
    }

    /// <summary>
    /// Retrieves the historical log of past and present logins for the authenticated user.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetSessionHistory(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var history = await _sessionService.GetSessionHistoryAsync(userId, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Terminates a specific remote session by its history ID.
    /// </summary>
    /// <param name="historyId">The unique identifier of the session history record.</param>
    [HttpDelete("{historyId}")]
    public async Task<IActionResult> LogoutSession(Guid historyId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _sessionService.LogoutSessionAsync(historyId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Terminates the current active session (the device making this request).
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutCurrentSession(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessionIdClaim = User.FindFirst("SessionId")?.Value;
        if (Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            await _sessionService.LogoutSessionAsync(sessionId, userId, cancellationToken);
        }
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid user token.");
    }
}
