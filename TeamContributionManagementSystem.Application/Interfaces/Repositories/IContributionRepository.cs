using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IContributionRepository
{
    Task<List<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default);
    Task<List<ContributionDto>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<List<ContributionDto>> GetByMemberEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default);
    void Update(Contribution contribution);
    void DeleteRange(IEnumerable<Contribution> contributions);

    // Standardized naming
    Task<List<ContributionDto>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<ContributionDto>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);
    void UpdateContributionAsyncById(Contribution contribution) => Update(contribution);
}
