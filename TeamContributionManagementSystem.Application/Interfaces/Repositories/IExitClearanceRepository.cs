using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IExitClearanceRepository
{
    Task<IReadOnlyCollection<ExitClearance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExitClearance?> GetByIdAsync(Guid clearanceId, CancellationToken cancellationToken = default);
    Task<ExitClearance?> GetByMemberAndEventAsync(Guid memberId, Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ExitClearance>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ExitClearance>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ExitClearance entity, CancellationToken cancellationToken = default);
    void Update(ExitClearance entity);

    Task AddHistoryAsync(ExitClearanceHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ExitClearanceHistory>> GetAllHistoryAsync(CancellationToken cancellationToken = default);
    Task<ExitClearanceHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Contribution>> GetMemberContributionsAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PaymentTransaction>> GetMemberPaymentsAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Event>> GetSendOffCandidateEventsAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetEventWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> MemberExistsAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void UpdateUser(AppUser user);
    Task<bool> HasActiveLoginSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
