using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IContributionRepository
{
    Task<List<Contribution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetByMemberEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default);
    void Update(Contribution contribution);
    void DeleteRange(IEnumerable<Contribution> contributions);

    // Standardized naming
    Task<List<Contribution>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<Contribution>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);
    void UpdateContributionAsyncById(Contribution contribution) => Update(contribution);
}
