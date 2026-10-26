using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class SupportTicketRepository : ISupportTicketRepository
{
    private static readonly SemaphoreSlim _ticketLock = new(1, 1);
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupportTicketRepository> _logger;

    public SupportTicketRepository(ApplicationDbContext context, ILogger<SupportTicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var allDbTickets = await _context.SupportTickets
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            bool needsSave = false;
            foreach (var ticket in allDbTickets)
            {
                if (string.IsNullOrWhiteSpace(ticket.TicketNo))
                {
                    ticket.TicketNo = await GenerateNextTicketNoAsync(cancellationToken);
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            var query = _context.SupportTickets.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanStatus = status.Trim().ToLower();
                query = query.Where(x => x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanStatus);
            }

            if (!string.IsNullOrWhiteSpace(ticketType) && !ticketType.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanType = ticketType.Trim().ToLower();
                query = query.Where(x => x.TicketTypeItem != null && x.TicketTypeItem.TypeName.ToLower() == cleanType);
            }

            if (!string.IsNullOrWhiteSpace(priority) && !priority.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanPriority = priority.Trim().ToLower();
                query = query.Where(x => x.PriorityItem != null && x.PriorityItem.PriorityName.ToLower() == cleanPriority);
            }

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupportTicketDto
                {
                    TicketId = x.TicketId,
                    TicketNo = x.TicketNo,
                    MemberName = x.User != null ? (!string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.FullName : x.User.Username) : string.Empty,
                    MemberId = x.UserId != null ? x.UserId.ToString() : null,
                    EventId = x.EventId,
                    RelatedEvent = x.Event != null ? x.Event.EventName : null,
                    TicketTypeId = x.TicketTypeId,
                    TicketType = x.TicketTypeItem != null ? x.TicketTypeItem.TypeName : string.Empty,
                    Subject = x.Subject,
                    Description = x.Description,
                    Status = x.StatusItem != null ? x.StatusItem.StatusName : string.Empty,
                    Priority = x.PriorityItem != null ? x.PriorityItem.PriorityName : string.Empty,
                    RefNo = x.RefNo,
                    Utr = x.Utr,
                    Attachment = x.Attachment,
                    ResolutionNotes = x.ResolutionNotes,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn,
                    VerifiedBy = !string.IsNullOrWhiteSpace(x.AssignedTo) ? x.AssignedTo : null
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                    if (string.IsNullOrWhiteSpace(item.VerifiedBy))
                    {
                        item.VerifiedBy = mName;
                    }
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

    public async Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SupportTickets
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.TicketTypeItem)
                .Include(x => x.PriorityItem)
                .Include(x => x.StatusItem)
                .FirstOrDefaultAsync(x => x.TicketId == ticketId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<SupportTicket?> GetByIdWithDeletedAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SupportTickets
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.TicketTypeItem)
                .Include(x => x.PriorityItem)
                .Include(x => x.StatusItem)
                .FirstOrDefaultAsync(x => x.TicketId == ticketId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdWithDeletedAsync));
            throw;
        }
    }

    public async Task<SupportTicket?> GetByTicketNoAsync(string ticketNo, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SupportTickets
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.TicketTypeItem)
                .Include(x => x.PriorityItem)
                .Include(x => x.StatusItem)
                .FirstOrDefaultAsync(x => x.TicketNo.ToLower() == ticketNo.ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByTicketNoAsync));
            throw;
        }
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SupportTickets.AddAsync(ticket, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(SupportTicket ticket)
    {
        try
        {
            _context.SupportTickets.Update(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(SupportTicket ticket)
    {
        try
        {
            ticket.IsDeleted = true;
            ticket.ModifiedOn = DateTime.UtcNow;
            _context.SupportTickets.Update(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }

    public async Task<string> GenerateNextTicketNoAsync(CancellationToken cancellationToken = default)
    {
        await _ticketLock.WaitAsync(cancellationToken);
        try
        {
            var baseDate = DateTime.UtcNow;
            var yearMonth = baseDate.ToString(CommonConstants.Defaults.TxnDateFormat); // "yyyyMM", e.g. "202610"
            var prefix = $"{CommonConstants.Defaults.TxnPrefix}{yearMonth}"; // e.g. "TXN202610"

            var existingTicketNumbers = await _context.SupportTickets
                .IgnoreQueryFilters()
                .Where(x => x.TicketNo.StartsWith(prefix))
                .Select(x => x.TicketNo)
                .ToListAsync(cancellationToken);

            long highestNumber = 0;

            foreach (var tNo in existingTicketNumbers)
            {
                if (string.IsNullOrWhiteSpace(tNo)) continue;
                string numPart = tNo.Length > prefix.Length ? tNo.Substring(prefix.Length) : tNo;
                var match = System.Text.RegularExpressions.Regex.Match(numPart, @"^\d+");
                if (match.Success && long.TryParse(match.Value, out var num))
                {
                    if (num > highestNumber) highestNumber = num;
                }
            }

            // Also check in-memory tracked entities in DbContext
            foreach (var local in _context.SupportTickets.Local)
            {
                if (!string.IsNullOrWhiteSpace(local.TicketNo) && local.TicketNo.StartsWith(prefix))
                {
                    string numPart = local.TicketNo.Length > prefix.Length ? local.TicketNo.Substring(prefix.Length) : local.TicketNo;
                    var match = System.Text.RegularExpressions.Regex.Match(numPart, @"^\d+");
                    if (match.Success && long.TryParse(match.Value, out var num))
                    {
                        if (num > highestNumber) highestNumber = num;
                    }
                }
            }

            long nextVal = highestNumber + 1;
            return $"{prefix}{nextVal:D4}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GenerateNextTicketNoAsync));
            throw;
        }
        finally
        {
            _ticketLock.Release();
        }
    }

    public async Task<string> GenerateNextRefNoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"REF-{dateStr}-";

            var refNumbers = await _context.SupportTickets
                .IgnoreQueryFilters()
                .Where(x => x.RefNo != null && x.RefNo.StartsWith(prefix))
                .Select(x => x.RefNo!)
                .ToListAsync(cancellationToken);

            int maxSeq = 0;
            foreach (var rNo in refNumbers)
            {
                var suffix = rNo.Substring(prefix.Length);
                if (int.TryParse(suffix, out int seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }

            return $"{prefix}{(maxSeq + 1):D3}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GenerateNextRefNoAsync));
            throw;
        }
    }
}
