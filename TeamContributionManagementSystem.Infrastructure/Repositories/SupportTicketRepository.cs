using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<SupportTicketRepository> _logger;

    public SupportTicketRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<SupportTicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SupportTicket>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SupportTickets.Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            query = query.Where(x => x.Status.ToLower() == status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(ticketType) && ticketType != "ALL")
        {
            query = query.Where(x => x.TicketType.ToLower() == ticketType.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(priority) && priority != "ALL")
        {
            query = query.Where(x => x.Priority.ToLower() == priority.ToLower());
        }

        return await query.OrderByDescending(x => x.CreatedOn).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
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
            _logger.LogError(ex, "Error in GetByIdAsync");
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
            _logger.LogError(ex, "Error in GetByTicketNoAsync");
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
            _logger.LogError(ex, "Error in AddAsync");
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
            _logger.LogError(ex, "Error in Update");
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
            _logger.LogError(ex, "Error in Delete");
            throw;
        }
    }
}
