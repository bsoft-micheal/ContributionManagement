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

    private static volatile bool _tableInitialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        if (_tableInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_tableInitialized) return;

            await _context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS birthday_reminder_logs (
    reminder_log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reminder_type VARCHAR(50) NOT NULL DEFAULT 'Scheduled',
    celebration_date TIMESTAMPTZ NOT NULL,
    celebrant_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    recipient_email VARCHAR(150) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Sent',
    sent_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    error_message VARCHAR(500) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_birthday_reminder_logs_dedup
ON birthday_reminder_logs (celebration_date, celebrant_user_id, recipient_user_id, reminder_type);
", Array.Empty<object>(), cancellationToken);

            _tableInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure birthday_reminder_logs table exists: {Message}", ex.Message);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<BirthdayReminderLog>> GetLogsForCelebrationDateAsync(DateTime celebrationDate, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);
        try
        {
            var utcDate = DateTime.SpecifyKind(celebrationDate.Date, DateTimeKind.Utc);
            var nextUtcDate = utcDate.AddDays(1);
            return await _context.BirthdayReminderLogs
                .AsNoTracking()
                .Where(x => x.CelebrationDate >= utcDate && x.CelebrationDate < nextUtcDate && x.Status == "Sent")
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query birthday reminder logs for celebration date: {Message}", ex.Message);
            return new List<BirthdayReminderLog>();
        }
    }

    public async Task<bool> HasBeenNotifiedAsync(DateTime celebrationDate, Guid celebrantUserId, Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);
        try
        {
            var utcDate = DateTime.SpecifyKind(celebrationDate.Date, DateTimeKind.Utc);
            var nextUtcDate = utcDate.AddDays(1);
            return await _context.BirthdayReminderLogs
                .AsNoTracking()
                .AnyAsync(x => x.CelebrationDate >= utcDate &&
                               x.CelebrationDate < nextUtcDate &&
                               x.CelebrantUserId == celebrantUserId &&
                               x.RecipientUserId == recipientUserId &&
                               x.Status == "Sent", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify HasBeenNotifiedAsync: {Message}", ex.Message);
            return false;
        }
    }

    public async Task AddAsync(BirthdayReminderLog log, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);
        try
        {
            // Normalize celebration date to UTC
            log.CelebrationDate = DateTime.SpecifyKind(log.CelebrationDate.Date, DateTimeKind.Utc);
            log.SentAt = DateTime.SpecifyKind(log.SentAt, DateTimeKind.Utc);

            await _context.BirthdayReminderLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Duplicate key - log entry already recorded, safe to ignore
            _logger.LogInformation("Birthday reminder log already exists for recipient {RecipientId} and celebrant {CelebrantId} (deduplicated).",
                log.RecipientUserId, log.CelebrantUserId);
            _context.Entry(log).State = EntityState.Detached;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist birthday reminder log: {Message}", ex.Message);
            _context.Entry(log).State = EntityState.Detached;
        }
    }

    public async Task AddRangeAsync(IEnumerable<BirthdayReminderLog> logs, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);
        try
        {
            foreach (var log in logs)
            {
                log.CelebrationDate = DateTime.SpecifyKind(log.CelebrationDate.Date, DateTimeKind.Utc);
                log.SentAt = DateTime.SpecifyKind(log.SentAt, DateTimeKind.Utc);
            }

            await _context.BirthdayReminderLogs.AddRangeAsync(logs, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx) when (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            _logger.LogInformation("Some birthday reminder logs were duplicates and already persisted.");
            foreach (var log in logs)
            {
                _context.Entry(log).State = EntityState.Detached;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist birthday reminder logs range: {Message}", ex.Message);
            foreach (var log in logs)
            {
                _context.Entry(log).State = EntityState.Detached;
            }
        }
    }
}
