using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(ApplicationDbContext context, ILogger<EventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events.Where(x => !x.IsDeleted).AsQueryable();

            if (month.HasValue)
            {
                query = query.Where(x => x.EventDate.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.EventDate.Year == year.Value);
            }

            var paymentTxnEventNames = new List<string>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                paymentTxnEventNames = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
                    .Select(x => x.Event!.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                expenseEventNames = await _context.Expenses
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load expense event names."); }

            try
            {
                photoEventNames = await _context.GalleryPhotos
                    .Where(x => !x.IsDeleted && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
                    .Select(x => x.Event!.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load photo event names."); }

            var results = await query
                .OrderByDescending(x => x.EventDate)
                .Select(x => new EventSummaryDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeId = x.EventTypeId,
                    EventTypeName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                    EventDate = x.EventDate,
                    EventDates = x.EventDates,
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount,
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusItem != null && c.StatusItem.StatusName.ToLower() == "paid").Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (c.StatusItem == null || c.StatusItem.StatusName.ToLower() != "paid")),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.Member!.IsDeleted).Select(p => new EventParticipantDto
                    {
                        Id = p.Id,
                        MemberId = p.UserId,
                        MemberName = p.Member != null ? p.Member.FullName : string.Empty,
                        RoleName = p.Member != null ? (p.Member.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member") : string.Empty
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var r in results)
            {
                var cleanName = r.EventName.Trim().ToLower();
                r.IsReferred = r.TotalPaidAmount > 0
                    || paymentTxnEventNames.Contains(cleanName)
                    || expenseEventNames.Contains(cleanName)
                    || photoEventNames.Contains(cleanName);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetAllAsync request was canceled.");
            return new List<EventSummaryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<EventSummaryDto>> GetAllForMemberAsync(Guid memberId, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events
                .Where(x => !x.IsDeleted && (
                    x.Contributions.Any(c => !c.IsDeleted && c.UserId == memberId) ||
                    x.Participants.Any(p => !p.IsDeleted && p.UserId == memberId)
                ))
                .AsQueryable();

            if (month.HasValue)
            {
                query = query.Where(x => x.EventDate.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.EventDate.Year == year.Value);
            }

            return await query
                .OrderByDescending(x => x.EventDate)
                .Select(x => new EventSummaryDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeId = x.EventTypeId,
                    EventTypeName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                    EventDate = x.EventDate,
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Contributions.Count(c => !c.IsDeleted && c.UserId == memberId) > 0 ? 1 : (x.Participants.Count(p => !p.IsDeleted && p.UserId == memberId) > 0 ? 1 : 0),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted && c.UserId == memberId).Sum(c => (decimal?)c.Amount) ?? 0m,
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.UserId == memberId && c.StatusItem != null && c.StatusItem.StatusName.ToLower() == "paid").Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.UserId == memberId && (c.StatusItem == null || c.StatusItem.StatusName.ToLower() != "paid")),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.Member!.IsDeleted && p.UserId == memberId).Select(p => new EventParticipantDto
                    {
                        Id = p.Id,
                        MemberId = p.UserId,
                        MemberName = p.Member != null ? p.Member.FullName : string.Empty,
                        RoleName = p.Member != null ? (p.Member.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member") : string.Empty
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllForMemberAsync));
            throw;
        }
    }

    public async Task<List<EventSummaryDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentTxnEventNames = new List<string>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                paymentTxnEventNames = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
                    .Select(x => x.Event!.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                expenseEventNames = await _context.Expenses
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load expense event names."); }

            try
            {
                photoEventNames = await _context.GalleryPhotos
                    .Where(x => !x.IsDeleted && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
                    .Select(x => x.Event!.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load photo event names."); }

            var results = await _context.Events
                .Where(x => !x.IsDeleted && x.EventDate >= DateTime.UtcNow.Date)
                .OrderBy(x => x.EventDate)
                .Take(count)
                .Select(x => new EventSummaryDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeId = x.EventTypeId,
                    EventTypeName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                    EventDate = x.EventDate,
                    EventDates = x.EventDates,
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount,
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusItem != null && c.StatusItem.StatusName.ToLower() == "paid").Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (c.StatusItem == null || c.StatusItem.StatusName.ToLower() != "paid")),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.Member!.IsDeleted).Select(p => new EventParticipantDto
                    {
                        Id = p.Id,
                        MemberId = p.UserId,
                        MemberName = p.Member != null ? p.Member.FullName : string.Empty,
                        RoleName = p.Member != null ? (p.Member.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member") : string.Empty
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var r in results)
            {
                var cleanName = r.EventName.Trim().ToLower();
                r.IsReferred = r.TotalPaidAmount > 0
                    || paymentTxnEventNames.Contains(cleanName)
                    || expenseEventNames.Contains(cleanName)
                    || photoEventNames.Contains(cleanName);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetUpcomingAsync request was canceled.");
            return new List<EventSummaryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetUpcomingAsync));
            throw;
        }
    }

    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .Include(x => x.Contributions)
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<Event?> GetByNameAsync(string eventName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .FirstOrDefaultAsync(x => x.EventName.ToLower() == eventName.Trim().ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByNameAsync));
            throw;
        }
    }

    public async Task<EventDetailsDto?> GetByIdWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .Where(x => x.EventId == eventId && !x.IsDeleted)
                .Select(x => new EventDetailsDto
                {
                    EventId = x.EventId,
                    EventName = x.EventName,
                    EventTypeId = x.EventTypeId,
                    EventTypeName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                    EventDate = x.EventDate,
                    EventDates = x.EventDates,
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount,
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusItem != null && c.StatusItem.StatusName.ToLower() == "paid").Sum(c => (decimal?)c.Amount) ?? 0m,
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.Member!.IsDeleted).Select(p => new EventParticipantDto
                    {
                        Id = p.Id,
                        MemberId = p.UserId,
                        MemberName = p.Member != null ? p.Member.FullName : string.Empty,
                        RoleName = p.Member != null ? (p.Member.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member") : string.Empty
                    }).ToList(),
                    Contributions = x.Contributions.Where(c => !c.IsDeleted).Select(c => new ContributionDto
                    {
                        ContributionId = c.ContributionId,
                        EventId = c.EventId,
                        EventName = x.EventName,
                        CategoryName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                        MemberId = c.UserId,
                        MemberName = c.User != null ? c.User.FullName : (c.Member != null ? c.Member.Name : string.Empty),
                        Amount = c.Amount,
                        PaymentStatus = (c.StatusItem != null && c.StatusItem.StatusName.ToLower() == "paid") ? PaymentStatus.Paid : PaymentStatus.Pending,
                        PaymentDate = c.PaymentDate,
                        PaymentMode = c.PaymentModeItem != null
                            ? (c.PaymentModeItem.IsCash ? PaymentMode.Cash : (c.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                            : PaymentMode.None,
                        CashAmount = c.CashAmount,
                        UpiAmount = c.UpiAmount,
                        CreatedBy = c.CreatedBy.HasValue ? c.CreatedBy.Value.ToString() : null,
                        CreatedAt = c.CreatedAt,
                        CreatedOn = c.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdWithDetailsAsync));
            throw;
        }
    }

    public async Task<bool> BirthdayEventExistsAsync(Guid memberId, int month, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .Include(x => x.EventType)
                .AnyAsync(x =>
                    x.EventType != null &&
                    x.EventType.EventTypeName == CommonConstants.EventTypeNames.Birthday &&
                    x.EventDate.Month == month &&
                    x.EventDate.Year == year &&
                    x.Description.Contains(memberId.ToString()),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(BirthdayEventExistsAsync));
            throw;
        }
    }

    public async Task<bool> HasPaidContributionsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Contributions.AnyAsync(x => x.EventId == eventId && !x.IsDeleted &&
                ((x.StatusItem != null && x.StatusItem.StatusName.ToLower() == "paid") || (x.Amount > 0 && (x.CashAmount > 0 || x.UpiAmount > 0))), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasPaidContributionsAsync));
            throw;
        }
    }

    public async Task<bool> HasPaymentTransactionsAsync(string eventName, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanName = eventName.Trim().ToLower();
            return await _context.PaymentTransactions.AnyAsync(x => !x.IsDeleted && x.Event != null && x.Event.EventName.ToLower() == cleanName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasPaymentTransactionsAsync));
            throw;
        }
    }

    public async Task<bool> HasExpensesAsync(string eventName, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanName = eventName.Trim().ToLower();
            return await _context.Expenses.AnyAsync(x => !x.IsDeleted && x.EventName.ToLower() == cleanName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasExpensesAsync));
            throw;
        }
    }

    public async Task<bool> HasGalleryPhotosAsync(string eventName, CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanName = eventName.Trim().ToLower();
            return await _context.GalleryPhotos.AnyAsync(x => !x.IsDeleted && x.Event != null && x.Event.EventName.ToLower() == cleanName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasGalleryPhotosAsync));
            throw;
        }
    }

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Events.AddAsync(eventItem, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(Event eventItem)
    {
        try
        {
            _context.Events.Update(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void DeleteParticipants(IEnumerable<EventParticipant> participants)
    {
        try
        {
            _context.EventParticipants.RemoveRange(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteParticipants));
            throw;
        }
    }
}
