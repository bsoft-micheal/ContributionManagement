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
            var query = _context.SupportTickets.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Status.ToLower() == status.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(ticketType) && !ticketType.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.TicketType.ToLower() == ticketType.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(priority) && !priority.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Priority.ToLower() == priority.ToLower());
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new SupportTicketDto
                {
                    TicketId = x.TicketId,
                    TicketNo = x.TicketNo,
                    MemberName = x.MemberName,
                    MemberId = x.MemberId,
                    RelatedEvent = x.RelatedEvent,
                    TicketType = x.TicketType,
                    Subject = x.Subject,
                    Description = x.Description,
                    Status = x.Status,
                    Priority = x.Priority,
                    AssignedTo = x.AssignedTo,
                    RefNo = x.RefNo,
                    Utr = x.Utr,
                    Attachment = x.Attachment,
                    ResolutionNotes = x.ResolutionNotes,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
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
            return await _context.SupportTickets.FirstOrDefaultAsync(x => x.TicketId == ticketId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<SupportTicket?> GetByTicketNoAsync(string ticketNo, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SupportTickets.FirstOrDefaultAsync(x => x.TicketNo.ToLower() == ticketNo.ToLower() && !x.IsDeleted, cancellationToken);
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
}
