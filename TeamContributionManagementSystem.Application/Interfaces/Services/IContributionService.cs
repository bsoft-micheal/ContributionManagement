using TeamContributionManagementSystem.Application.DTOs.Contributions;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IContributionService
{
    Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<ContributionDto>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<IReadOnlyCollection<ContributionDto>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);
    Task<ContributionDto> SavePayContributionAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default) => PayAsync(request, cancellationToken);
}
