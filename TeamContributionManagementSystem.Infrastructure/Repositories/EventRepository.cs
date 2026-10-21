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
            var paymentTxnEventIds = new List<Guid>();
            var contribPaidEventIds = new List<Guid>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                var paymentTxns = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && (
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        !string.IsNullOrEmpty(x.VerifiedBy) ||
                        x.VerifiedOn != null ||
                        x.Amount > 0
                    ) && x.Amount > 0)
                    .Select(x => new {
                        EventId = x.EventId,
                        EventName = x.Event != null ? x.Event.EventName.ToLower() : null
                    })
                    .ToListAsync(cancellationToken);

                paymentTxnEventIds = paymentTxns.Where(p => p.EventId.HasValue).Select(p => p.EventId!.Value).Distinct().ToList();
                paymentTxnEventNames = paymentTxns.Where(p => !string.IsNullOrEmpty(p.EventName)).Select(p => p.EventName!).Distinct().ToList();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                contribPaidEventIds = await _context.Contributions
                    .Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0) ||
                        (c.StatusItem != null && (
                            c.StatusItem.StatusName.ToLower().Contains("paid") ||
                            c.StatusItem.StatusName.ToLower().Contains("verif") ||
                            c.StatusItem.StatusName.ToLower().Contains("approv") ||
                            c.StatusItem.StatusName.ToLower() == "closed" ||
                            c.StatusItem.StatusName.ToLower() == "completed"
                        ))
                    ))
                    .Select(c => c.EventId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load contributions paid event ids."); }

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

            var ticketCountByEventId = new Dictionary<Guid, int>();
            var ticketCountByEventName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var activeTickets = await _context.SupportTickets
                    .Where(t => !t.IsDeleted &&
                        (t.StatusItem == null || (
                            !t.StatusItem.StatusName.ToLower().Contains("resolve") &&
                            !t.StatusItem.StatusName.ToLower().Contains("close")
                        )) &&
                        (t.EventId.HasValue || (t.Event != null && t.Event.EventId != Guid.Empty)))
                    .Select(t => new {
                        EventId = t.EventId ?? (t.Event != null ? t.Event.EventId : Guid.Empty),
                        EventName = t.Event != null ? t.Event.EventName : string.Empty
                    })
                    .ToListAsync(cancellationToken);

                ticketCountByEventId = activeTickets
                    .Where(t => t.EventId != Guid.Empty)
                    .GroupBy(t => t.EventId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ticketCountByEventName = activeTickets
                    .Where(t => !string.IsNullOrEmpty(t.EventName))
                    .GroupBy(t => t.EventName.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load support ticket counts."); }

            var results = await query
                .OrderByDescending(x => x.EventDate)
                .ThenByDescending(x => x.CreatedAt)
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
                    UseAvailableBalance = x.UseAvailableBalance,
                    AllocatedAmount = x.AllocatedAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.UseAvailableBalance && x.AllocatedAmount > 0
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? Math.Max(0m, x.BaseAmount - x.AllocatedAmount))
                        : Math.Max(x.BaseAmount, x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )).Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (c.StatusItem == null || (c.StatusItem.StatusName.ToLower() != "paid" && c.StatusItem.StatusName.ToLower() != "verified" && c.StatusItem.StatusName.ToLower() != "closed" && c.StatusItem.StatusName.ToLower() != "completed")) && c.PaymentDate == null && (!c.CashAmount.HasValue || c.CashAmount.Value <= 0) && (!c.UpiAmount.HasValue || c.UpiAmount.Value <= 0)),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
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
                int count = 0;
                if (ticketCountByEventId.TryGetValue(r.EventId, out var c1))
                {
                    count = c1;
                }
                else if (!string.IsNullOrEmpty(r.EventName) && ticketCountByEventName.TryGetValue(r.EventName.Trim().ToLower(), out var c2))
                {
                    count = c2;
                }
                r.SupportTicketRaisedCount = count;
                var cleanName = r.EventName.Trim().ToLower();
                bool hasPaymentTxn = paymentTxnEventIds.Contains(r.EventId) || paymentTxnEventNames.Contains(cleanName);
                bool hasPaidContrib = contribPaidEventIds.Contains(r.EventId);
                r.HasPaidContributions = r.HasPaidContributions || hasPaidContrib || hasPaymentTxn || r.TotalPaidAmount > 0 || r.PaidContributionsCount > 0;
                if (r.HasPaidContributions && r.PaidContributionsCount == 0)
                {
                    r.PaidContributionsCount = 1;
                }
                r.IsReferred = r.HasPaidContributions
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

    public async Task<List<EventSummaryDto>> GetAllForOrganizerAsync(Guid organizerUserId, int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events
                .Where(x => !x.IsDeleted && (
                    x.CreatedBy == organizerUserId ||
                    x.Participants.Any(p => !p.IsDeleted && p.UserId == organizerUserId)
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

            var paymentTxnEventNames = new List<string>();
            var paymentTxnEventIds = new List<Guid>();
            var contribPaidEventIds = new List<Guid>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                var paymentTxns = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && (
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        !string.IsNullOrEmpty(x.VerifiedBy) ||
                        x.VerifiedOn != null ||
                        x.Amount > 0
                    ) && x.Amount > 0)
                    .Select(x => new {
                        EventId = x.EventId,
                        EventName = x.Event != null ? x.Event.EventName.ToLower() : null
                    })
                    .ToListAsync(cancellationToken);

                paymentTxnEventIds = paymentTxns.Where(p => p.EventId.HasValue).Select(p => p.EventId!.Value).Distinct().ToList();
                paymentTxnEventNames = paymentTxns.Where(p => !string.IsNullOrEmpty(p.EventName)).Select(p => p.EventName!).Distinct().ToList();
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                contribPaidEventIds = await _context.Contributions
                    .Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0) ||
                        (c.StatusItem != null && (
                            c.StatusItem.StatusName.ToLower().Contains("paid") ||
                            c.StatusItem.StatusName.ToLower().Contains("verif") ||
                            c.StatusItem.StatusName.ToLower().Contains("approv") ||
                            c.StatusItem.StatusName.ToLower() == "closed" ||
                            c.StatusItem.StatusName.ToLower() == "completed"
                        ))
                    ))
                    .Select(c => c.EventId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load contributions paid event ids."); }

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
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load photo event names."); }

            var ticketCountByEventId = new Dictionary<Guid, int>();
            var ticketCountByEventName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var activeTickets = await _context.SupportTickets
                    .Where(t => !t.IsDeleted &&
                        (t.StatusItem == null || (
                            !t.StatusItem.StatusName.ToLower().Contains("resolve") &&
                            !t.StatusItem.StatusName.ToLower().Contains("close")
                        )) &&
                        (t.EventId.HasValue || (t.Event != null && t.Event.EventId != Guid.Empty)))
                    .Select(t => new {
                        EventId = t.EventId ?? (t.Event != null ? t.Event.EventId : Guid.Empty),
                        EventName = t.Event != null ? t.Event.EventName : string.Empty
                    })
                    .ToListAsync(cancellationToken);

                ticketCountByEventId = activeTickets
                    .Where(t => t.EventId != Guid.Empty)
                    .GroupBy(t => t.EventId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ticketCountByEventName = activeTickets
                    .Where(t => !string.IsNullOrEmpty(t.EventName))
                    .GroupBy(t => t.EventName.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load support ticket counts."); }

            var results = await query
                .OrderByDescending(x => x.EventDate)
                .ThenByDescending(x => x.CreatedAt)
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
                    UseAvailableBalance = x.UseAvailableBalance,
                    AllocatedAmount = x.AllocatedAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.UseAvailableBalance && x.AllocatedAmount > 0
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? Math.Max(0m, x.BaseAmount - x.AllocatedAmount))
                        : Math.Max(x.BaseAmount, x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )).Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (c.StatusItem == null || (c.StatusItem.StatusName.ToLower() != "paid" && c.StatusItem.StatusName.ToLower() != "verified" && c.StatusItem.StatusName.ToLower() != "closed" && c.StatusItem.StatusName.ToLower() != "completed")) && c.PaymentDate == null && (!c.CashAmount.HasValue || c.CashAmount.Value <= 0) && (!c.UpiAmount.HasValue || c.UpiAmount.Value <= 0)),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
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
                int count = 0;
                if (ticketCountByEventId.TryGetValue(r.EventId, out var c1))
                {
                    count = c1;
                }
                else if (!string.IsNullOrEmpty(r.EventName) && ticketCountByEventName.TryGetValue(r.EventName.Trim().ToLower(), out var c2))
                {
                    count = c2;
                }
                r.SupportTicketRaisedCount = count;
                var cleanName = r.EventName.Trim().ToLower();
                bool hasPaymentTxn = paymentTxnEventIds.Contains(r.EventId) || paymentTxnEventNames.Contains(cleanName);
                bool hasPaidContrib = contribPaidEventIds.Contains(r.EventId);
                r.HasPaidContributions = r.HasPaidContributions || hasPaidContrib || hasPaymentTxn || r.TotalPaidAmount > 0 || r.PaidContributionsCount > 0;
                if (r.HasPaidContributions && r.PaidContributionsCount == 0)
                {
                    r.PaidContributionsCount = 1;
                }
                r.IsReferred = r.HasPaidContributions
                    || expenseEventNames.Contains(cleanName)
                    || photoEventNames.Contains(cleanName);
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetAllForOrganizerAsync request was canceled.");
            return new List<EventSummaryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllForOrganizerAsync));
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

            var results = await query
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
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.UserId == memberId && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )).Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.UserId == memberId && (c.StatusItem == null || (c.StatusItem.StatusName.ToLower() != "paid" && c.StatusItem.StatusName.ToLower() != "verified" && c.StatusItem.StatusName.ToLower() != "closed" && c.StatusItem.StatusName.ToLower() != "completed")) && c.PaymentDate == null && (!c.CashAmount.HasValue || c.CashAmount.Value <= 0) && (!c.UpiAmount.HasValue || c.UpiAmount.Value <= 0)),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
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

            var ticketCountByEventId = new Dictionary<Guid, int>();
            var ticketCountByEventName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var activeTickets = await _context.SupportTickets
                    .Where(t => !t.IsDeleted &&
                        (t.StatusItem == null || (
                            !t.StatusItem.StatusName.ToLower().Contains("resolve") &&
                            !t.StatusItem.StatusName.ToLower().Contains("close")
                        )) &&
                        (t.EventId.HasValue || (t.Event != null && t.Event.EventId != Guid.Empty)))
                    .Select(t => new {
                        EventId = t.EventId ?? (t.Event != null ? t.Event.EventId : Guid.Empty),
                        EventName = t.Event != null ? t.Event.EventName : string.Empty
                    })
                    .ToListAsync(cancellationToken);

                ticketCountByEventId = activeTickets
                    .Where(t => t.EventId != Guid.Empty)
                    .GroupBy(t => t.EventId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ticketCountByEventName = activeTickets
                    .Where(t => !string.IsNullOrEmpty(t.EventName))
                    .GroupBy(t => t.EventName.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load support ticket counts for member events."); }

            foreach (var r in results)
            {
                int count = 0;
                if (ticketCountByEventId.TryGetValue(r.EventId, out var c1))
                {
                    count = c1;
                }
                else if (!string.IsNullOrEmpty(r.EventName) && ticketCountByEventName.TryGetValue(r.EventName.Trim().ToLower(), out var c2))
                {
                    count = c2;
                }
                r.SupportTicketRaisedCount = count;
                r.HasPaidContributions = r.HasPaidContributions || r.TotalPaidAmount > 0 || r.PaidContributionsCount > 0;
                if (r.HasPaidContributions && r.PaidContributionsCount == 0)
                {
                    r.PaidContributionsCount = 1;
                }
            }

            return results;
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
                    .Where(x => !x.IsDeleted && (
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        !string.IsNullOrEmpty(x.VerifiedBy) ||
                        x.VerifiedOn != null
                    ) && x.Amount > 0 && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
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

            var ticketCountByEventId = new Dictionary<Guid, int>();
            var ticketCountByEventName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var activeTickets = await _context.SupportTickets
                    .Where(t => !t.IsDeleted &&
                        (t.StatusItem == null || (
                            !t.StatusItem.StatusName.ToLower().Contains("resolve") &&
                            !t.StatusItem.StatusName.ToLower().Contains("close")
                        )) &&
                        (t.EventId.HasValue || (t.Event != null && t.Event.EventId != Guid.Empty)))
                    .Select(t => new {
                        EventId = t.EventId ?? (t.Event != null ? t.Event.EventId : Guid.Empty),
                        EventName = t.Event != null ? t.Event.EventName : string.Empty
                    })
                    .ToListAsync(cancellationToken);

                ticketCountByEventId = activeTickets
                    .Where(t => t.EventId != Guid.Empty)
                    .GroupBy(t => t.EventId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ticketCountByEventName = activeTickets
                    .Where(t => !string.IsNullOrEmpty(t.EventName))
                    .GroupBy(t => t.EventName.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load support ticket counts for upcoming events."); }

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
                    UseAvailableBalance = x.UseAvailableBalance,
                    AllocatedAmount = x.AllocatedAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.UseAvailableBalance && x.AllocatedAmount > 0
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? Math.Max(0m, x.BaseAmount - x.AllocatedAmount))
                        : Math.Max(x.BaseAmount, x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )).Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (c.StatusItem == null || (c.StatusItem.StatusName.ToLower() != "paid" && c.StatusItem.StatusName.ToLower() != "verified" && c.StatusItem.StatusName.ToLower() != "closed" && c.StatusItem.StatusName.ToLower() != "completed")) && c.PaymentDate == null && (!c.CashAmount.HasValue || c.CashAmount.Value <= 0) && (!c.UpiAmount.HasValue || c.UpiAmount.Value <= 0)),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
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
                int ticketCount = 0;
                if (ticketCountByEventId.TryGetValue(r.EventId, out var c1))
                {
                    ticketCount = c1;
                }
                else if (!string.IsNullOrEmpty(r.EventName) && ticketCountByEventName.TryGetValue(r.EventName.Trim().ToLower(), out var c2))
                {
                    ticketCount = c2;
                }
                r.SupportTicketRaisedCount = ticketCount;
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
                    .ThenInclude(c => c.StatusItem)
                .Include(x => x.Contributions)
                    .ThenInclude(c => c.PaymentModeItem)
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public Task<Event?> GetByNameAsync(string eventName, CancellationToken cancellationToken = default)
        => GetByNameAsync(eventName, null, cancellationToken);

    public async Task<Event?> GetByNameAsync(string eventName, string? category, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events
                .Include(x => x.EventType)
                .Where(x => x.EventName.ToLower() == eventName.Trim().ToLower() && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(category))
            {
                var catTrim = category.Trim().ToLower();
                var categoryFiltered = query.Where(x => x.EventType != null && x.EventType.EventTypeName.ToLower() == catTrim);
                var matchedWithCategory = await categoryFiltered
                    .OrderByDescending(x => x.EventDate)
                    .ThenByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (matchedWithCategory != null)
                {
                    return matchedWithCategory;
                }
            }

            return await query
                .OrderByDescending(x => x.EventDate)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
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
            var details = await _context.Events
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
                    UseAvailableBalance = x.UseAvailableBalance,
                    AllocatedAmount = x.AllocatedAmount,
                    ParticipantCount = x.Participants.Count(p => !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.UseAvailableBalance && x.AllocatedAmount > 0
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? Math.Max(0m, x.BaseAmount - x.AllocatedAmount))
                        : Math.Max(x.BaseAmount, x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )).Sum(c => (decimal?)c.Amount) ?? 0m,
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && (
                        c.PaymentDate != null ||
                        (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )),
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
                        PaymentStatus = (c.PaymentDate != null || (c.StatusItem != null && (c.StatusItem.StatusName.ToLower() == "paid" || c.StatusItem.StatusName.ToLower() == "verified" || c.StatusItem.StatusName.ToLower() == "closed" || c.StatusItem.StatusName.ToLower() == "completed")) || (c.CashAmount.HasValue && c.CashAmount.Value > 0) || (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)) ? PaymentStatus.Paid : PaymentStatus.Pending,
                        StatusName = c.StatusItem != null ? c.StatusItem.StatusName : (c.PaymentDate != null ? "Paid" : "Pending"),
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

            if (details != null)
            {
                try
                {
                    var cleanEventName = details.EventName.Trim().ToLower();
                    details.SupportTicketRaisedCount = await _context.SupportTickets
                        .CountAsync(t => !t.IsDeleted &&
                            (t.EventId == details.EventId || (t.Event != null && t.Event.EventId == details.EventId) || (t.Event != null && t.Event.EventName.ToLower() == cleanEventName)) &&
                            (t.StatusItem == null || (
                                !t.StatusItem.StatusName.ToLower().Contains("resolve") &&
                                !t.StatusItem.StatusName.ToLower().Contains("close")
                            )), cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to load support ticket count for event details."); }

                var cleanName = details.EventName.Trim().ToLower();
                var hasTxn = await _context.PaymentTransactions.AnyAsync(x => !x.IsDeleted && x.Amount > 0 &&
                    (x.EventId == details.EventId || (x.Event != null && x.Event.EventId == details.EventId)), cancellationToken);

                var hasContrib = details.Contributions.Any(c =>
                    c.PaymentDate != null ||
                    (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                    (c.UpiAmount.HasValue && c.UpiAmount.Value > 0) ||
                    (!string.IsNullOrEmpty(c.StatusName) && (
                        c.StatusName.ToLower().Contains("paid") ||
                        c.StatusName.ToLower().Contains("verif") ||
                        c.StatusName.ToLower().Contains("approv") ||
                        c.StatusName.ToLower() == "closed" ||
                        c.StatusName.ToLower() == "completed"
                    ))
                );

                details.HasPaidContributions = details.HasPaidContributions || hasTxn || hasContrib || details.TotalPaidAmount > 0 || details.PaidContributionsCount > 0;
                if (details.HasPaidContributions && details.PaidContributionsCount == 0)
                {
                    details.PaidContributionsCount = 1;
                }
            }

            return details;
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
            var eventEntity = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
            var cleanName = eventEntity?.EventName?.Trim().ToLower();

            var hasPaidContrib = await _context.Contributions.AnyAsync(x => x.EventId == eventId && !x.IsDeleted &&
                (
                    x.PaymentDate != null ||
                    (x.StatusItem != null && (
                        x.StatusItem.StatusName.ToLower().Contains("paid") ||
                        x.StatusItem.StatusName.ToLower().Contains("verif") ||
                        x.StatusItem.StatusName.ToLower().Contains("approv") ||
                        x.StatusItem.StatusName.ToLower() == "closed" ||
                        x.StatusItem.StatusName.ToLower() == "completed"
                    )) ||
                    (x.CashAmount.HasValue && x.CashAmount.Value > 0) ||
                    (x.UpiAmount.HasValue && x.UpiAmount.Value > 0)
                ), cancellationToken);

            if (hasPaidContrib) return true;

            var hasPaymentTxn = await _context.PaymentTransactions
                .AnyAsync(x => !x.IsDeleted && x.Amount > 0 &&
                    (
                        x.EventId == eventId ||
                        (x.Event != null && x.Event.EventId == eventId) ||
                        (cleanName != null && x.Event != null && x.Event.EventName.ToLower() == cleanName)
                    ), cancellationToken);

            return hasPaymentTxn;
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
