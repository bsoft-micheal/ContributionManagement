using TeamContributionManagementSystem.Application.DTOs.Auth;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for managing user sessions and active device details.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Retrieves all currently active device sessions for a specified user.
    /// </summary>
    /// <param name="userId">The unique ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active device detail records.</returns>
    Task<IEnumerable<DeviceSessionDto>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the login session history for a specified user.
    /// </summary>
    /// <param name="userId">The unique ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of historical device detail records.</returns>
    Task<IEnumerable<DeviceSessionDto>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out and invalidates a specific active session.
    /// </summary>
    /// <param name="historyId">The session history identifier.</param>
    /// <param name="userId">The unique ID of the user performing the logout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutSessionAsync(Guid historyId, Guid userId, CancellationToken cancellationToken = default);
}

