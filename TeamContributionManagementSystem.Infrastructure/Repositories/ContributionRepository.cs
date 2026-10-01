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
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Contributions
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
                    PaymentStatus = ((x.PaymentDate != null && (x.PaymentModeId != null || x.CashAmount > 0 || x.UpiAmount > 0)) || (x.StatusItem != null && (x.StatusItem.StatusName.ToLower() == "paid" || x.StatusItem.StatusName.ToLower() == "verified" || x.StatusItem.StatusName.ToLower() == "closed" || x.StatusItem.StatusName.ToLower() == "completed"))) ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : (x.CashAmount > 0 && x.UpiAmount > 0 ? PaymentMode.Split : (x.CashAmount > 0 ? PaymentMode.Cash : (x.UpiAmount > 0 ? PaymentMode.Upi : (x.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
            }

            return items;
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
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Contributions
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
                    PaymentStatus = ((x.PaymentDate != null && (x.PaymentModeId != null || x.CashAmount > 0 || x.UpiAmount > 0)) || (x.StatusItem != null && (x.StatusItem.StatusName.ToLower() == "paid" || x.StatusItem.StatusName.ToLower() == "verified" || x.StatusItem.StatusName.ToLower() == "closed" || x.StatusItem.StatusName.ToLower() == "completed"))) ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : (x.CashAmount > 0 && x.UpiAmount > 0 ? PaymentMode.Split : (x.CashAmount > 0 ? PaymentMode.Cash : (x.UpiAmount > 0 ? PaymentMode.Upi : (x.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                var ev = await _context.Events
                    .Include(e => e.Participants)
                    .Include(e => e.EventType)
                    .FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted, cancellationToken);

                if (ev != null)
                {
                    var participantUserIds = ev.Participants.Where(p => !p.IsDeleted).Select(p => p.UserId).Distinct().ToList();
                    if (participantUserIds.Count == 0)
                    {
                        participantUserIds = await _context.Users
                            .Where(u => !u.IsDeleted && u.IsActive)
                            .Select(u => u.UserId)
                            .ToListAsync(cancellationToken);
                    }

                    if (participantUserIds.Count > 0)
                    {
                        var baseAmount = ev.BaseAmount;
                        var perMemberAmount = participantUserIds.Count > 0 ? Math.Round(baseAmount / participantUserIds.Count, 2) : 0m;

                        var newContribs = new List<Contribution>();
                        foreach (var uid in participantUserIds)
                        {
                            var contrib = new Contribution
                            {
                                ContributionId = Guid.NewGuid(),
                                EventId = ev.EventId,
                                UserId = uid,
                                Amount = perMemberAmount,
                                PaymentStatus = PaymentStatus.Pending,
                                PaymentMode = PaymentMode.None,
                                IsActive = true,
                                IsDeleted = false,
                                CreatedBy = ev.CreatedBy,
                                CreatedAt = ev.CreatedAt
                            };
                            newContribs.Add(contrib);
                        }

                        await _context.Contributions.AddRangeAsync(newContribs, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        items = await _context.Contributions
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
                                PaymentStatus = ((x.PaymentDate != null && (x.PaymentModeId != null || x.CashAmount > 0 || x.UpiAmount > 0)) || (x.StatusItem != null && (x.StatusItem.StatusName.ToLower() == "paid" || x.StatusItem.StatusName.ToLower() == "verified" || x.StatusItem.StatusName.ToLower() == "closed" || x.StatusItem.StatusName.ToLower() == "completed"))) ? PaymentStatus.Paid : PaymentStatus.Pending,
                                PaymentDate = x.PaymentDate,
                                PaymentMode = x.PaymentModeItem != null
                                    ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                                    : (x.CashAmount > 0 && x.UpiAmount > 0 ? PaymentMode.Split : (x.CashAmount > 0 ? PaymentMode.Cash : (x.UpiAmount > 0 ? PaymentMode.Upi : (x.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
                                CashAmount = x.CashAmount,
                                UpiAmount = x.UpiAmount,
                                CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                                CreatedAt = x.CreatedAt,
                                CreatedOn = x.CreatedAt
                            })
                            .ToListAsync(cancellationToken);
                    }
                }
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.MemberName) && users.TryGetValue(item.MemberId, out var mName))
                {
                    item.MemberName = mName;
                }
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
            }

            return items;
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

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await query
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
                    PaymentStatus = ((x.PaymentDate != null && (x.PaymentModeId != null || x.CashAmount > 0 || x.UpiAmount > 0)) || (x.StatusItem != null && (x.StatusItem.StatusName.ToLower() == "paid" || x.StatusItem.StatusName.ToLower() == "verified" || x.StatusItem.StatusName.ToLower() == "closed" || x.StatusItem.StatusName.ToLower() == "completed"))) ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : (x.CashAmount > 0 && x.UpiAmount > 0 ? PaymentMode.Split : (x.CashAmount > 0 ? PaymentMode.Cash : (x.UpiAmount > 0 ? PaymentMode.Upi : (x.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
            }

            return items;
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

    public async Task AddAsync(Contribution contribution, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Contributions.AddAsync(contribution, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
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
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Contributions
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
                    PaymentStatus = ((x.PaymentDate != null && (x.PaymentModeId != null || x.CashAmount > 0 || x.UpiAmount > 0)) || (x.StatusItem != null && (x.StatusItem.StatusName.ToLower() == "paid" || x.StatusItem.StatusName.ToLower() == "verified" || x.StatusItem.StatusName.ToLower() == "closed" || x.StatusItem.StatusName.ToLower() == "completed"))) ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null
                        ? (x.PaymentModeItem.IsCash ? PaymentMode.Cash : (x.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                        : (x.CashAmount > 0 && x.UpiAmount > 0 ? PaymentMode.Split : (x.CashAmount > 0 ? PaymentMode.Cash : (x.UpiAmount > 0 ? PaymentMode.Upi : (x.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
                    CashAmount = x.CashAmount,
                    UpiAmount = x.UpiAmount,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByMemberEmailAsync));
            throw;
        }
    }
}
