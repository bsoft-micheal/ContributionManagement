using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IBirthdayReminderLogRepository
{
    Task<List<BirthdayReminderLog>> GetLogsForCelebrationDateAsync(DateTime celebrationDate, CancellationToken cancellationToken = default);
    Task<bool> HasBeenNotifiedAsync(DateTime celebrationDate, Guid celebrantUserId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task AddAsync(BirthdayReminderLog log, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<BirthdayReminderLog> logs, CancellationToken cancellationToken = default);
}
