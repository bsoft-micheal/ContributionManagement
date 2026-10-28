using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class ExitClearanceRepository : IExitClearanceRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExitClearanceRepository> _logger;

    public ExitClearanceRepository(ApplicationDbContext context, ILogger<ExitClearanceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task EnsureTablesExistAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync(@"
CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

CREATE TABLE IF NOT EXISTS exit_clearances (
    clearance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    member_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE RESTRICT,
    status VARCHAR(50) NOT NULL DEFAULT 'PendingClearance',
    planned_exit_date TIMESTAMPTZ NULL,
    notes TEXT NULL,
    approval_remarks TEXT NULL,
    approved_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    approved_at TIMESTAMPTZ NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL,
    modified_on TIMESTAMPTZ NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_exit_clearances_member_event
ON exit_clearances (member_id, event_id)
WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ix_exit_clearances_member_id ON exit_clearances(member_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearances_event_id ON exit_clearances(event_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearances_status ON exit_clearances(status);

CREATE TABLE IF NOT EXISTS exit_clearance_histories (
    history_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    clearance_id UUID NOT NULL REFERENCES exit_clearances(clearance_id) ON DELETE RESTRICT,
    member_id UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    event_id UUID NOT NULL REFERENCES events(event_id) ON DELETE RESTRICT,
    member_name_snapshot VARCHAR(200) NOT NULL,
    member_code_snapshot VARCHAR(100) NULL,
    event_name_snapshot VARCHAR(200) NOT NULL,
    joining_date TIMESTAMPTZ NULL,
    exit_approval_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_by UUID NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    approved_by_name VARCHAR(200) NOT NULL,
    total_obligations NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_verified_paid NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_approved_adjustments NUMERIC(12,2) NOT NULL DEFAULT 0,
    outstanding_at_approval NUMERIC(12,2) NOT NULL DEFAULT 0,
    payment_verification_summary TEXT NULL,
    approval_remarks TEXT NULL,
    snapshot_json TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID NULL REFERENCES users(user_id) ON DELETE SET NULL
);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_member_id ON exit_clearance_histories(member_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_event_id ON exit_clearance_histories(event_id);
CREATE INDEX IF NOT EXISTS ix_exit_clearance_histories_clearance_id ON exit_clearance_histories(clearance_id);
", cancellationToken);

            // Ensure any exited members whose exit clearance was approved remain active and usable
            await _context.Database.ExecuteSqlRawAsync(@"
UPDATE users 
SET is_active = true, is_exited = false 
WHERE user_id IN (SELECT member_id FROM exit_clearances WHERE status = 'Approved') 
  AND (is_active = false OR is_exited = true);
", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureTablesExistAsync: non-fatal table check note");
        }
    }

    public async Task<IReadOnlyCollection<ExitClearance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearances
            .AsNoTracking()
            .Include(x => x.Member)
                .ThenInclude(m => m!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(x => x.Event)
                .ThenInclude(e => e!.EventType)
            .Include(x => x.ApprovedByUser)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExitClearance?> GetByIdAsync(Guid clearanceId, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearances
            .Include(x => x.Member)
                .ThenInclude(m => m!.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(x => x.Event)
                .ThenInclude(e => e!.EventType)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.Histories)
            .FirstOrDefaultAsync(x => x.ClearanceId == clearanceId && !x.IsDeleted, cancellationToken);
    }

    public async Task<ExitClearance?> GetByMemberAndEventAsync(Guid memberId, Guid eventId, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearances
            .Include(x => x.Member)
            .Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.MemberId == memberId && x.EventId == eventId && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExitClearance>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearances
            .Include(x => x.Event)
            .Where(x => x.MemberId == memberId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExitClearance>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearances
            .Include(x => x.Member)
            .Where(x => x.EventId == eventId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExitClearance entity, CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);
        await _context.ExitClearances.AddAsync(entity, cancellationToken);
    }

    public void Update(ExitClearance entity)
    {
        _context.ExitClearances.Update(entity);
    }

    public async Task AddHistoryAsync(ExitClearanceHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ExitClearanceHistories.AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExitClearanceHistory>> GetAllHistoryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.ExitClearanceHistories
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.Event)
            .Include(x => x.ApprovedByUser)
            .OrderByDescending(x => x.ExitApprovalDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExitClearanceHistory?> GetHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        return await _context.ExitClearanceHistories
            .Include(x => x.Member)
            .Include(x => x.Event)
            .Include(x => x.ApprovedByUser)
            .FirstOrDefaultAsync(x => x.HistoryId == historyId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Contribution>> GetMemberContributionsAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Contributions
            .Include(c => c.Event)
                .ThenInclude(e => e!.EventType)
            .Include(c => c.StatusItem)
            .Where(c => c.UserId == memberId && !c.IsDeleted && c.Event != null && !c.Event.IsDeleted && c.Event.Status != EventStatus.Draft)
            .OrderBy(c => c.Event!.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentTransaction>> GetMemberPaymentsAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTransactions
            .Include(p => p.Event)
            .Include(p => p.PaymentModeItem)
            .Include(p => p.StatusItem)
            .Where(p => p.UserId == memberId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Event>> GetSendOffCandidateEventsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTablesExistAsync(cancellationToken);

        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Contributions)
            .Include(e => e.Participants)
            .Where(e => !e.IsDeleted && e.Status != EventStatus.Draft)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetEventWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.Contributions)
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<bool> MemberExistsAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.UserId == memberId && !u.IsDeleted, cancellationToken);
    }

    public async Task<AppUser?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<bool> HasActiveLoginSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceLoginHistories
            .AnyAsync(h => h.UserId == userId && h.IsActive && h.LogoutTime == null, cancellationToken);
    }

    public void UpdateUser(AppUser user)
    {
        user.IsActive = true;
        user.IsExited = false;
        _context.Users.Update(user);
    }
}
