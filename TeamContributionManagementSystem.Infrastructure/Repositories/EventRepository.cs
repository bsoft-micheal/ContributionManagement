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
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount),
                    CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.FullName : null,
                    CreatedBy = x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null,
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
                        MemberId = p.MemberId,
                        MemberName = p.Member != null ? p.Member.Name : string.Empty,
                        RoleName = (p.Member != null && p.Member.Role != null) ? p.Member.Role.RoleName : string.Empty
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<EventSummaryDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
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
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount),
                    CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.FullName : null,
                    CreatedBy = x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null,
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
                        MemberId = p.MemberId,
                        MemberName = p.Member != null ? p.Member.Name : string.Empty,
                        RoleName = (p.Member != null && p.Member.Role != null) ? p.Member.Role.RoleName : string.Empty
                    }).ToList()
                })
                .ToListAsync(cancellationToken);
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
            return await _context.Events.FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
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
                    Description = x.Description,
                    Status = x.Status,
                    BaseAmount = x.BaseAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Where(c => !c.IsDeleted).Sum(c => c.Amount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.PaymentStatus == PaymentStatus.Paid).Sum(c => c.Amount),
                    CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.FullName : null,
                    CreatedBy = x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null,
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
                        MemberId = p.MemberId,
                        MemberName = p.Member != null ? p.Member.Name : string.Empty,
                        RoleName = (p.Member != null && p.Member.Role != null) ? p.Member.Role.RoleName : string.Empty
                    }).ToList(),
                    Contributions = x.Contributions.Where(c => !c.IsDeleted).Select(c => new ContributionDto
                    {
                        ContributionId = c.ContributionId,
                        EventId = c.EventId,
                        EventName = x.EventName,
                        CategoryName = x.EventType != null ? x.EventType.EventTypeName : string.Empty,
                        MemberId = c.MemberId,
                        MemberName = c.Member != null ? c.Member.Name : string.Empty,
                        Amount = c.Amount,
                        PaymentStatus = c.PaymentStatus,
                        PaymentDate = c.PaymentDate,
                        PaymentMode = c.PaymentMode,
                        CashAmount = c.CashAmount,
                        UpiAmount = c.UpiAmount,
                        CreatedBy = c.CreatedBy,
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
