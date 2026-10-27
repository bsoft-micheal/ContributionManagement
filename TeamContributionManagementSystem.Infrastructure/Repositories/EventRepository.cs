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

    private async Task<List<Guid>> GetPaidStatusIdsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Statuses
                .AsNoTracking()
                .Where(s => !s.IsDeleted && (
                    s.StatusName.ToLower() == "paid" ||
                    s.StatusName.ToLower() == "verified" ||
                    s.StatusName.ToLower() == "approved" ||
                    s.StatusName.ToLower() == "closed" ||
                    s.StatusName.ToLower() == "completed"
                ))
                .Select(s => s.StatusId)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return new List<Guid>();
        }
    }

    public async Task<List<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var paidStatusIds = await GetPaidStatusIdsAsync(cancellationToken);
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
            var paymentTxnAmountByEventId = new Dictionary<Guid, decimal>();
            var paymentTxnAmountByEventName = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var contribPaidEventIds = new List<Guid>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                var paymentTxns = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && (
                        (x.StatusId.HasValue && paidStatusIds.Contains(x.StatusId.Value)) ||
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        !string.IsNullOrEmpty(x.VerifiedBy) ||
                        x.VerifiedOn != null
                    ) && x.Amount > 0)
                    .Select(x => new {
                        EventId = x.EventId,
                        EventName = x.Event != null ? x.Event.EventName.ToLower() : null,
                        Amount = x.Amount
                    })
                    .ToListAsync(cancellationToken);

                paymentTxnEventIds = paymentTxns.Where(p => p.EventId.HasValue).Select(p => p.EventId!.Value).Distinct().ToList();
                paymentTxnEventNames = paymentTxns.Where(p => !string.IsNullOrEmpty(p.EventName)).Select(p => p.EventName!).Distinct().ToList();

                paymentTxnAmountByEventId = paymentTxns
                    .Where(p => p.EventId.HasValue)
                    .GroupBy(p => p.EventId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                paymentTxnAmountByEventName = paymentTxns
                    .Where(p => !string.IsNullOrEmpty(p.EventName))
                    .GroupBy(p => p.EventName!.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                contribPaidEventIds = await _context.Contributions
                    .Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                    .Select(c => c.EventId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load contributions paid event ids."); }

            try
            {
                expenseEventNames = await _context.Expenses
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load expense event names."); }

            try
            {
                photoEventNames = await _context.GalleryPhotos
                    .Where(x => !x.IsDeleted && x.Event != null && !string.IsNullOrEmpty(x.Event.EventName))
                    .Select(x => x.Event!.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
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
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
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
                    ParticipantCount = x.Participants.Count(p => !p.IsDeleted && !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Any(c => !c.IsDeleted)
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? 0m)
                        : (x.UseAvailableBalance && x.AllocatedAmount > 0
                            ? Math.Max(0m, x.BaseAmount - x.AllocatedAmount)
                            : x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                        .Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted &&
                        (!c.StatusId.HasValue || !paidStatusIds.Contains(c.StatusId.Value))
                    ),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    PublishedBy = x.PublishedBy,
                    PublishedByName = x.PublishedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.PublishedByUser.FullName) ? x.PublishedByUser.FullName : x.PublishedByUser.Username)
                        : null,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.IsDeleted && !p.Member!.IsDeleted).Select(p => new EventParticipantDto
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
                var cleanName = (r.EventName ?? string.Empty).Trim().ToLower();

                decimal txAmount = 0m;
                if (r.EventId != Guid.Empty && paymentTxnAmountByEventId.TryGetValue(r.EventId, out var amt1))
                {
                    txAmount = amt1;
                }
                else if (!string.IsNullOrEmpty(cleanName) && paymentTxnAmountByEventName.TryGetValue(cleanName, out var amt2))
                {
                    txAmount = amt2;
                }

                if (txAmount > r.TotalPaidAmount)
                {
                    r.TotalPaidAmount = txAmount;
                }

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
                EnrichEventAdjustmentMetadata(r);
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
            var paidStatusIds = await GetPaidStatusIdsAsync(cancellationToken);
            var query = _context.Events
                .Where(x => !x.IsDeleted && x.CreatedBy == organizerUserId)
                .AsQueryable();

            // Multi-role session isolation: Check if this organizer user ALSO has the Admin role assigned
            var isUserAdmin = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == organizerUserId && ur.Role != null && !ur.Role.IsDeleted && ur.Role.RoleName.ToLower() == CommonRoles.Admin.ToLower(), cancellationToken);

            if (isUserAdmin)
            {
                // Multi-role user operating in Organizer role: only show events created under the Organizer role
                query = query.Where(x => x.Description != null && x.Description.Contains("CREATOR_ROLE: Organizer"));
            }

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
            var paymentTxnAmountByEventId = new Dictionary<Guid, decimal>();
            var paymentTxnAmountByEventName = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var contribPaidEventIds = new List<Guid>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();

            try
            {
                var paymentTxns = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && (
                        (x.StatusId.HasValue && paidStatusIds.Contains(x.StatusId.Value)) ||
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        !string.IsNullOrEmpty(x.VerifiedBy) ||
                        x.VerifiedOn != null
                    ) && x.Amount > 0)
                    .Select(x => new {
                        EventId = x.EventId,
                        EventName = x.Event != null ? x.Event.EventName.ToLower() : null,
                        Amount = x.Amount
                    })
                    .ToListAsync(cancellationToken);

                paymentTxnEventIds = paymentTxns.Where(p => p.EventId.HasValue).Select(p => p.EventId!.Value).Distinct().ToList();
                paymentTxnEventNames = paymentTxns.Where(p => !string.IsNullOrEmpty(p.EventName)).Select(p => p.EventName!).Distinct().ToList();

                paymentTxnAmountByEventId = paymentTxns
                    .Where(p => p.EventId.HasValue)
                    .GroupBy(p => p.EventId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                paymentTxnAmountByEventName = paymentTxns
                    .Where(p => !string.IsNullOrEmpty(p.EventName))
                    .GroupBy(p => p.EventName!.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount), StringComparer.OrdinalIgnoreCase);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load payment transaction event names."); }

            try
            {
                contribPaidEventIds = await _context.Contributions
                    .Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                    .Select(c => c.EventId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load contributions paid event ids."); }

            try
            {
                expenseEventNames = await _context.Expenses
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load expense event names."); }

            try
            {
                photoEventNames = await _context.GalleryPhotos
                    .Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.EventName))
                    .Select(x => x.EventName.ToLower())
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
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
            catch (OperationCanceledException) { return new List<EventSummaryDto>(); }
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
                    ParticipantCount = x.Participants.Count(p => !p.IsDeleted && !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Any(c => !c.IsDeleted)
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? 0m)
                        : (x.UseAvailableBalance && x.AllocatedAmount > 0
                            ? Math.Max(0m, x.BaseAmount - x.AllocatedAmount)
                            : x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                        .Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted &&
                        (!c.StatusId.HasValue || !paidStatusIds.Contains(c.StatusId.Value))
                    ),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    PublishedBy = x.PublishedBy,
                    PublishedByName = x.PublishedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.PublishedByUser.FullName) ? x.PublishedByUser.FullName : x.PublishedByUser.Username)
                        : null,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.IsDeleted && !p.Member!.IsDeleted).Select(p => new EventParticipantDto
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
                string eventNameLower = (r.EventName ?? string.Empty).Trim().ToLower();

                decimal txAmount = 0m;
                if (r.EventId != Guid.Empty && paymentTxnAmountByEventId.TryGetValue(r.EventId, out var amt1))
                {
                    txAmount = amt1;
                }
                else if (!string.IsNullOrEmpty(eventNameLower) && paymentTxnAmountByEventName.TryGetValue(eventNameLower, out var amt2))
                {
                    txAmount = amt2;
                }

                if (txAmount > r.TotalPaidAmount)
                {
                    r.TotalPaidAmount = txAmount;
                }

                bool hasPaymentTxn = paymentTxnEventIds.Contains(r.EventId) || paymentTxnEventNames.Contains(eventNameLower);
                bool hasPaidContrib = contribPaidEventIds.Contains(r.EventId);
                r.HasPaidContributions = r.HasPaidContributions || hasPaidContrib || hasPaymentTxn || r.TotalPaidAmount > 0 || r.PaidContributionsCount > 0;
                if (r.HasPaidContributions && r.PaidContributionsCount == 0)
                {
                    r.PaidContributionsCount = 1;
                }
                r.IsReferred = r.HasPaidContributions
                    || expenseEventNames.Contains(eventNameLower)
                    || photoEventNames.Contains(eventNameLower);
                EnrichEventAdjustmentMetadata(r);
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
            var paidStatusIds = await GetPaidStatusIdsAsync(cancellationToken);
            var query = _context.Events
                .Where(x => !x.IsDeleted && x.Status != EventStatus.Draft && (
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
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.UserId == memberId && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                        .Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.UserId == memberId &&
                        (!c.StatusId.HasValue || !paidStatusIds.Contains(c.StatusId.Value))
                    ),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.UserId == memberId && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && c.UserId == memberId && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    PublishedBy = x.PublishedBy,
                    PublishedByName = x.PublishedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.PublishedByUser.FullName) ? x.PublishedByUser.FullName : x.PublishedByUser.Username)
                        : null,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.IsDeleted && !p.Member!.IsDeleted && p.UserId == memberId).Select(p => new EventParticipantDto
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
                EnrichEventAdjustmentMetadata(r);
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
            var paidStatusIds = await GetPaidStatusIdsAsync(cancellationToken);
            var paymentTxnEventNames = new List<string>();
            var paymentTxnEventIds = new List<Guid>();
            var expenseEventNames = new List<string>();
            var photoEventNames = new List<string>();
            var paymentTxns = new List<dynamic>();

            var paymentTxnAmountByEventId = new Dictionary<Guid, decimal>();
            var paymentTxnAmountByEventName = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var txns = await _context.PaymentTransactions
                    .Where(x => !x.IsDeleted && (
                        (x.StatusId.HasValue && paidStatusIds.Contains(x.StatusId.Value)) ||
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower() == "paid" ||
                            x.StatusItem.StatusName.ToLower() == "verified" ||
                            x.StatusItem.StatusName.ToLower() == "approved" ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        (!string.IsNullOrEmpty(x.VerifiedBy) && (x.StatusItem == null || x.StatusItem.StatusName.ToLower() != "rejected")) ||
                        x.VerifiedOn != null
                    ) && x.Amount > 0)
                    .Select(x => new {
                        EventId = x.EventId,
                        EventName = x.Event != null ? x.Event.EventName.ToLower() : null,
                        Amount = x.Amount
                    })
                    .ToListAsync(cancellationToken);

                paymentTxnEventIds = txns.Where(p => p.EventId.HasValue).Select(p => p.EventId!.Value).Distinct().ToList();
                paymentTxnEventNames = txns.Where(p => !string.IsNullOrEmpty(p.EventName)).Select(p => p.EventName!).Distinct().ToList();

                paymentTxnAmountByEventId = txns
                    .Where(p => p.EventId.HasValue)
                    .GroupBy(p => p.EventId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

                paymentTxnAmountByEventName = txns
                    .Where(p => !string.IsNullOrEmpty(p.EventName))
                    .GroupBy(p => p.EventName!.Trim().ToLower())
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount), StringComparer.OrdinalIgnoreCase);
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
                .Where(x => !x.IsDeleted && x.Status != EventStatus.Draft && x.EventDate >= DateTime.UtcNow.Date)
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
                    ParticipantCount = x.Participants.Count(p => !p.IsDeleted && !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Any(c => !c.IsDeleted)
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? 0m)
                        : (x.UseAvailableBalance && x.AllocatedAmount > 0
                            ? Math.Max(0m, x.BaseAmount - x.AllocatedAmount)
                            : x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                        .Sum(c => (decimal?)c.Amount) ?? 0m,
                    PendingContributionsCount = x.Contributions.Count(c => !c.IsDeleted &&
                        (!c.StatusId.HasValue || !paidStatusIds.Contains(c.StatusId.Value))
                    ),
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    PublishedBy = x.PublishedBy,
                    PublishedByName = x.PublishedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.PublishedByUser.FullName) ? x.PublishedByUser.FullName : x.PublishedByUser.Username)
                        : null,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.IsDeleted && !p.Member!.IsDeleted).Select(p => new EventParticipantDto
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
                var cleanName = (r.EventName ?? string.Empty).Trim().ToLower();

                decimal txAmount = 0m;
                if (r.EventId != Guid.Empty && paymentTxnAmountByEventId.TryGetValue(r.EventId, out var amt1))
                {
                    txAmount = amt1;
                }
                else if (!string.IsNullOrEmpty(cleanName) && paymentTxnAmountByEventName.TryGetValue(cleanName, out var amt2))
                {
                    txAmount = amt2;
                }

                if (txAmount > r.TotalPaidAmount)
                {
                    r.TotalPaidAmount = txAmount;
                }

                r.HasPaidContributions = r.HasPaidContributions || r.TotalPaidAmount > 0 || r.PaidContributionsCount > 0 || paymentTxnEventIds.Contains(r.EventId) || paymentTxnEventNames.Contains(cleanName);
                if (r.HasPaidContributions && r.PaidContributionsCount == 0)
                {
                    r.PaidContributionsCount = 1;
                }

                r.IsReferred = r.TotalPaidAmount > 0
                    || paymentTxnEventNames.Contains(cleanName)
                    || expenseEventNames.Contains(cleanName)
                    || photoEventNames.Contains(cleanName);
                EnrichEventAdjustmentMetadata(r);
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
            var paidStatusIds = await GetPaidStatusIdsAsync(cancellationToken);
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
                    ParticipantCount = x.Participants.Count(p => !p.IsDeleted && !p.Member!.IsDeleted),
                    TotalExpectedAmount = x.Contributions.Any(c => !c.IsDeleted)
                        ? (x.Contributions.Where(c => !c.IsDeleted).Sum(c => (decimal?)c.Amount) ?? 0m)
                        : (x.UseAvailableBalance && x.AllocatedAmount > 0
                            ? Math.Max(0m, x.BaseAmount - x.AllocatedAmount)
                            : x.BaseAmount),
                    TotalPaidAmount = x.Contributions.Where(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value))
                        .Sum(c => (decimal?)c.Amount) ?? 0m,
                    PaidContributionsCount = x.Contributions.Count(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    HasPaidContributions = x.Contributions.Any(c => !c.IsDeleted && c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)),
                    CreatedByName = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : null,
                    CreatedBy = x.CreatedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.CreatedByUser.FullName) ? x.CreatedByUser.FullName : x.CreatedByUser.Username)
                        : (x.CreatedBy != Guid.Empty ? x.CreatedBy.ToString() : null),
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    PublishedAt = x.PublishedAt,
                    PublishedBy = x.PublishedBy,
                    PublishedByName = x.PublishedByUser != null
                        ? (!string.IsNullOrWhiteSpace(x.PublishedByUser.FullName) ? x.PublishedByUser.FullName : x.PublishedByUser.Username)
                        : null,
                    HasTenureRule = x.EventType != null && x.EventType.HasTenureRule,
                    TenureThresholdYears = x.EventType != null ? x.EventType.TenureThresholdYears : 0,
                    NewEntrantSharePercentage = x.EventType != null ? x.EventType.NewEntrantSharePercentage : 0,
                    StandardSharePercentage = x.EventType != null ? x.EventType.StandardSharePercentage : 0,
                    RuleDescription = x.EventType != null ? x.EventType.RuleDescription : null,
                    Participants = x.Participants.Where(p => !p.IsDeleted && !p.Member!.IsDeleted).Select(p => new EventParticipantDto
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
                        PaymentStatus = (c.StatusId.HasValue && paidStatusIds.Contains(c.StatusId.Value)) ? PaymentStatus.Paid : PaymentStatus.Pending,
                        StatusName = c.StatusItem != null ? c.StatusItem.StatusName : "Pending",
                        PaymentDate = c.PaymentDate,
                        PaymentMode = c.PaymentModeItem != null
                            ? (c.PaymentModeItem.IsCash ? PaymentMode.Cash : (c.PaymentModeItem.PaymentType == "Split" ? PaymentMode.Split : PaymentMode.Upi))
                            : (c.CashAmount > 0 && c.UpiAmount > 0 ? PaymentMode.Split : (c.CashAmount > 0 ? PaymentMode.Cash : (c.UpiAmount > 0 ? PaymentMode.Upi : (c.PaymentDate != null ? PaymentMode.Cash : PaymentMode.None)))),
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
                decimal txSum = 0m;
                try
                {
                    txSum = await _context.PaymentTransactions
                        .Where(x => !x.IsDeleted && x.Amount > 0 &&
                            (
                                (x.StatusItem != null && (
                                    x.StatusItem.StatusName.ToLower() == "paid" ||
                                    x.StatusItem.StatusName.ToLower() == "verified" ||
                                    x.StatusItem.StatusName.ToLower() == "approved" ||
                                    x.StatusItem.StatusName.ToLower() == "completed"
                                )) ||
                                !string.IsNullOrEmpty(x.VerifiedBy) ||
                                x.VerifiedOn != null
                            ) &&
                            (x.EventId == details.EventId || (x.Event != null && (x.Event.EventId == details.EventId || x.Event.EventName.ToLower() == cleanName))))
                        .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load payment transactions sum for event details.");
                }

                if (txSum > details.TotalPaidAmount)
                {
                    details.TotalPaidAmount = txSum;
                }

                var hasTxn = txSum > 0;

                var hasContrib = details.Contributions.Any(c =>
                    !string.IsNullOrEmpty(c.StatusName) && (
                        c.StatusName.ToLower() == "paid" ||
                        c.StatusName.ToLower() == "verified" ||
                        c.StatusName.ToLower() == "approved" ||
                        c.StatusName.ToLower() == "closed" ||
                        c.StatusName.ToLower() == "completed"
                    )
                );

                details.HasPaidContributions = details.HasPaidContributions || hasTxn || hasContrib || details.TotalPaidAmount > 0 || details.PaidContributionsCount > 0;
                if (details.HasPaidContributions && details.PaidContributionsCount == 0)
                {
                    details.PaidContributionsCount = 1;
                }
                EnrichEventAdjustmentMetadata(details);
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

    public async Task<bool> HasDuplicateEventTypeInMonthAsync(Guid eventTypeId, int month, int year, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetTypeId = eventTypeId;
            var targetType = await _context.EventTypes.AsNoTracking().FirstOrDefaultAsync(t => t.EventTypeId == targetTypeId, cancellationToken);
            var targetTypeName = targetType?.EventTypeName?.Trim().ToLowerInvariant() ?? string.Empty;

            var existingEvents = await _context.Events
                .AsNoTracking()
                .Include(e => e.EventType)
                .Where(e => !e.IsDeleted && (!excludeEventId.HasValue || e.EventId != excludeEventId.Value))
                .Select(e => new
                {
                    e.EventId,
                    e.EventTypeId,
                    EventTypeName = e.EventType != null ? e.EventType.EventTypeName : string.Empty,
                    e.EventName,
                    e.EventDate,
                    e.EventDates
                })
                .ToListAsync(cancellationToken);

            foreach (var ev in existingEvents)
            {
                var evTypeName = ev.EventTypeName.Trim().ToLowerInvariant();
                var evName = (ev.EventName ?? string.Empty).Trim().ToLowerInvariant();
                var isSameType = ev.EventTypeId == targetTypeId ||
                    (!string.IsNullOrWhiteSpace(targetTypeName) && !string.IsNullOrWhiteSpace(evTypeName) &&
                     evTypeName.Replace(" ", "").Replace("-", "").Replace("_", "").Equals(
                         targetTypeName.Replace(" ", "").Replace("-", "").Replace("_", ""),
                         StringComparison.OrdinalIgnoreCase)) ||
                    (targetTypeName.Contains("birthday") && (evTypeName.Contains("birthday") || evName.Contains("birthday")));

                // Check primary event type and primary date
                if (isSameType && ev.EventDate.Month == month && ev.EventDate.Year == year)
                {
                    return true;
                }

                // Check serialized dates & multi-types in EventDates JSON
                if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(ev.EventDates);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            bool typeMatches = false;
                            if (Guid.TryParse(prop.Name, out var parsedTypeId) && parsedTypeId == targetTypeId)
                            {
                                typeMatches = true;
                            }
                            else if (!string.IsNullOrWhiteSpace(targetTypeName) &&
                                     prop.Name.Replace(" ", "").Replace("-", "").Replace("_", "").Equals(
                                         targetTypeName.Replace(" ", "").Replace("-", "").Replace("_", ""),
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                typeMatches = true;
                            }

                            if (typeMatches)
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var dateStr = prop.Value.GetString();
                                    if (DateTime.TryParse(dateStr, out var parsedDate) && parsedDate.Month == month && parsedDate.Year == year)
                                    {
                                        return true;
                                    }
                                }
                                else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    foreach (var subProp in prop.Value.EnumerateObject())
                                    {
                                        if (subProp.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            var dateStr = subProp.Value.GetString();
                                            if (DateTime.TryParse(dateStr, out var parsedDate) && parsedDate.Month == month && parsedDate.Year == year)
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Even if type doesn't match prop.Name, check if this event has primary type == targetTypeId
                                // and one of the sub-dates is in target month/year
                                if (isSameType)
                                {
                                    if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        var dateStr = prop.Value.GetString();
                                        if (DateTime.TryParse(dateStr, out var parsedDate) && parsedDate.Month == month && parsedDate.Year == year)
                                        {
                                            return true;
                                        }
                                    }
                                    else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                                    {
                                        foreach (var subProp in prop.Value.EnumerateObject())
                                        {
                                            if (subProp.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                                            {
                                                var dateStr = subProp.Value.GetString();
                                                if (DateTime.TryParse(dateStr, out var parsedDate) && parsedDate.Month == month && parsedDate.Year == year)
                                                {
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Continue checking other events */ }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasDuplicateEventTypeInMonthAsync));
            throw;
        }
    }

    public async Task<List<Event>> GetActiveEventsInMonthAsync(int month, int year, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Participants)
                    .ThenInclude(p => p.Member)
                .Where(e => !e.IsDeleted && (!excludeEventId.HasValue || e.EventId != excludeEventId.Value));

            var allCandidates = await query.ToListAsync(cancellationToken);
            var results = new List<Event>();

            foreach (var ev in allCandidates)
            {
                if (ev.EventDate.Month == month && ev.EventDate.Year == year)
                {
                    results.Add(ev);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(ev.EventDates);
                        bool inMonth = false;
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                if (DateTime.TryParse(prop.Value.GetString(), out var d) && d.Month == month && d.Year == year)
                                {
                                    inMonth = true;
                                    break;
                                }
                            }
                            else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                foreach (var sub in prop.Value.EnumerateObject())
                                {
                                    if (sub.Value.ValueKind == System.Text.Json.JsonValueKind.String &&
                                        DateTime.TryParse(sub.Value.GetString(), out var sd) && sd.Month == month && sd.Year == year)
                                    {
                                        inMonth = true;
                                        break;
                                    }
                                }
                                if (inMonth) break;
                            }
                        }
                        if (inMonth)
                        {
                            results.Add(ev);
                        }
                    }
                    catch { }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetActiveEventsInMonthAsync));
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
                    (x.StatusItem != null && (
                        x.StatusItem.StatusName.ToLower().Contains("paid") ||
                        x.StatusItem.StatusName.ToLower().Contains("verif") ||
                        x.StatusItem.StatusName.ToLower().Contains("approv") ||
                        x.StatusItem.StatusName.ToLower() == "closed" ||
                        x.StatusItem.StatusName.ToLower() == "completed"
                    ))
                ), cancellationToken);

            if (hasPaidContrib) return true;

            var hasPaymentTxn = await _context.PaymentTransactions
                .AnyAsync(x => !x.IsDeleted && x.Amount > 0 &&
                    (
                        (x.StatusItem != null && (
                            x.StatusItem.StatusName.ToLower().Contains("paid") ||
                            x.StatusItem.StatusName.ToLower().Contains("verif") ||
                            x.StatusItem.StatusName.ToLower().Contains("approv") ||
                            x.StatusItem.StatusName.ToLower() == "completed"
                        )) ||
                        (!string.IsNullOrEmpty(x.VerifiedBy) && (x.StatusItem == null || x.StatusItem.StatusName.ToLower() != "rejected"))
                    ) &&
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

    public async Task AddParticipantsAsync(IEnumerable<EventParticipant> participants, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.EventParticipants.AddRangeAsync(participants, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddParticipantsAsync));
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

    private static void EnrichEventAdjustmentMetadata(EventSummaryDto? dto)
    {
        if (dto == null) return;
        var (cleanDesc, usePrev, adjAmt, adjType, origBudget, _) = EventMetadataHelper.UnpackDescription(dto.Description);
        dto.Description = cleanDesc;
        dto.UsePreviousEventBalance = usePrev;
        dto.PreviousEventAdjustment = adjAmt;
        dto.PreviousEventAdjustmentType = adjType;
        dto.OriginalPlannedBudget = origBudget > 0 ? origBudget : dto.BaseAmount;
    }
}
