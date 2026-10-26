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

    public async Task<IReadOnlyCollection<ExitClearance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ExitClearances
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
        return await _context.ExitClearances
            .Include(x => x.Member)
            .Include(x => x.Event)
            .FirstOrDefaultAsync(x => x.MemberId == memberId && x.EventId == eventId && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExitClearance>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.ExitClearances
            .Include(x => x.Event)
            .Where(x => x.MemberId == memberId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExitClearance>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.ExitClearances
            .Include(x => x.Member)
            .Where(x => x.EventId == eventId && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ExitClearance entity, CancellationToken cancellationToken = default)
    {
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
        return await _context.ExitClearanceHistories
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
            .Where(p => p.UserId == memberId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Event>> GetSendOffCandidateEventsAsync(CancellationToken cancellationToken = default)
    {
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
}
