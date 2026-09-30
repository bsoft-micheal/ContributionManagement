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
                    MemberId = x.UserId,
                    MemberName = x.User != null ? x.User.FullName : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = (x.StatusItem != null && x.StatusItem.StatusName.ToLower() == "paid") ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null 
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : PaymentMode.None,
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
                .OrderBy(x => x.User != null ? x.User.FullName : string.Empty)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.UserId,
                    MemberName = x.User != null ? x.User.FullName : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = (x.StatusItem != null && x.StatusItem.StatusName.ToLower() == "paid") ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null 
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : PaymentMode.None,
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
                .Include(x => x.User)
                .Include(x => x.StatusItem)
                .Include(x => x.PaymentModeItem)
                .FirstOrDefaultAsync(x => x.EventId == eventId && x.UserId == memberId && !x.IsDeleted, cancellationToken);
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
                .Where(x => !x.IsDeleted && (x.StatusItem == null || x.StatusItem.StatusName.ToLower() != "paid") && x.Event != null && !x.Event.IsDeleted);

            if (month.HasValue)
            {
                query = query.Where(x => x.Event!.EventDate.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.Event!.EventDate.Year == year.Value);
            }

            return await query
                .OrderBy(x => x.Event!.EventDate)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.UserId,
                    MemberName = x.User != null ? x.User.FullName : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = (x.StatusItem != null && x.StatusItem.StatusName.ToLower() == "paid") ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null 
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : PaymentMode.None,
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
                .Where(x => !x.IsDeleted && x.User != null && x.User.Email == email)
                .OrderBy(x => x.Event!.EventDate)
                .Select(x => new ContributionDto
                {
                    ContributionId = x.ContributionId,
                    EventId = x.EventId,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    CategoryName = (x.Event != null && x.Event.EventType != null) ? x.Event.EventType.EventTypeName : string.Empty,
                    MemberId = x.UserId,
                    MemberName = x.User != null ? x.User.FullName : string.Empty,
                    Amount = x.Amount,
                    PaymentStatus = (x.StatusItem != null && x.StatusItem.StatusName.ToLower() == "paid") ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null 
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : PaymentMode.None,
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
