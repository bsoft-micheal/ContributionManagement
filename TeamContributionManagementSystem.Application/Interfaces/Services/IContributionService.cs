using TeamContributionManagementSystem.Application.DTOs.Contributions;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for processing event contributions and member payment records.
/// </summary>
public interface IContributionService
{
    /// <summary>
    /// Retrieves all contribution records across events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of contribution DTOs.</returns>
    Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves contribution records for a specific event.
    /// </summary>
    /// <param name="eventId">The unique ID of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of contribution DTOs for the specified event.</returns>
    Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a payment for an event contribution.
    /// </summary>
    /// <param name="request">The payment details request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated contribution DTO.</returns>
    Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the personal contribution summary for a logged-in member by their email.
    /// </summary>
    /// <param name="userEmail">The member's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary of paid, pending, and total contributions for the member.</returns>
    Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Standardized alias method to retrieve all contribution records.
    /// </summary>
    Task<IReadOnlyCollection<ContributionDto>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);

    /// <summary>
    /// Standardized alias method to retrieve contributions by event ID.
    /// </summary>
    Task<IReadOnlyCollection<ContributionDto>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);

    /// <summary>
    /// Standardized alias method to record a payment contribution.
    /// </summary>
    Task<ContributionDto> SavePayContributionAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default) => PayAsync(request, cancellationToken);
}
