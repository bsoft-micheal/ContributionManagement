using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class BirthdayReminderLogRepository : IBirthdayReminderLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BirthdayReminderLogRepository> _logger;

    public BirthdayReminderLogRepository(ApplicationDbContext context, ILogger<BirthdayReminderLogRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BirthdayReminderLog>> GetLogsForCelebrationDateAsync(DateTime celebrationDate, CancellationToken cancellationToken = default)
    {
        var dateOnly = celebrationDate.Date;
        return await _context.BirthdayReminderLogs
            .AsNoTracking()
            .Where(x => x.CelebrationDate.Date == dateOnly && x.Status == "Sent")
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasBeenNotifiedAsync(DateTime celebrationDate, Guid celebrantUserId, Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        var dateOnly = celebrationDate.Date;
        return await _context.BirthdayReminderLogs
            .AsNoTracking()
            .AnyAsync(x => x.CelebrationDate.Date == dateOnly &&
                           x.CelebrantUserId == celebrantUserId &&
                           x.RecipientUserId == recipientUserId &&
                           x.Status == "Sent", cancellationToken);
    }

    public async Task AddAsync(BirthdayReminderLog log, CancellationToken cancellationToken = default)
    {
        await _context.BirthdayReminderLogs.AddAsync(log, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<BirthdayReminderLog> logs, CancellationToken cancellationToken = default)
    {
        await _context.BirthdayReminderLogs.AddRangeAsync(logs, cancellationToken);
    }
}
