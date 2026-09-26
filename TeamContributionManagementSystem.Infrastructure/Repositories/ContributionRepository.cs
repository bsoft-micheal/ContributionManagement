using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ContributionRepository> _logger;

    public ContributionRepository(ApplicationDbContext context, ILogger<ContributionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contributions
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Event!.EventDate)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.MemberId,
                    MemberName = x.Member != null ? x.Member.Name : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentMode,
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contributions
                .Where(x => x.EventId == eventId && !x.IsDeleted)
                .OrderBy(x => x.Member!.Name)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.MemberId,
                    MemberName = x.Member != null ? x.Member.Name : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentMode,
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEventIdAsync));
            throw;
        }
    }

    public async Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contributions
                .Include(x => x.Event)
                .Include(x => x.Member)
                .FirstOrDefaultAsync(x => x.EventId == eventId && x.MemberId == memberId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEventAndMemberAsync));
            throw;
        }
    }

    public async Task<List<ContributionDto>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Contributions
                .Where(x => !x.IsDeleted && x.PaymentStatus != PaymentStatus.Paid);

            if (month.HasValue)
            {
                query = query.Where(x => x.Event != null && x.Event.EventDate.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.Event != null && x.Event.EventDate.Year == year.Value);
            }

            return await query
                .OrderBy(x => x.Event!.EventDate)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.MemberId,
                    MemberName = x.Member != null ? x.Member.Name : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentMode,
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetPendingAsync));
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Contributions.AddRangeAsync(contributions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddRangeAsync));
            throw;
        }
    }

    public void Update(Contribution contribution)
    {
        try
        {
            _context.Contributions.Update(contribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void DeleteRange(IEnumerable<Contribution> contributions)
    {
        try
        {
            _context.Contributions.RemoveRange(contributions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteRange));
            throw;
        }
    }

    public async Task<List<ContributionDto>> GetByMemberEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contributions
                .Where(x => !x.IsDeleted && x.Member != null && x.Member.Email == email)
                .OrderBy(x => x.Event!.EventDate)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.MemberId,
                    MemberName = x.Member != null ? x.Member.Name : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentMode,
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByMemberEmailAsync));
            throw;
        }
    }
}
