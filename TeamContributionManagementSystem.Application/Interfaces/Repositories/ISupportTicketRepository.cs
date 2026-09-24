using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface ISupportTicketRepository
{
    Task<List<SupportTicket>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByTicketNoAsync(string ticketNo, CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    void Update(SupportTicket ticket);
    void Delete(SupportTicket ticket);

    // Standardized naming
    Task<List<SupportTicket>> GetAllSupportTicketAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default) => GetAllAsync(status, ticketType, priority, cancellationToken);
    Task<SupportTicket?> GetSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => GetByIdAsync(ticketId, cancellationToken);
    Task SaveSupportTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default) => AddAsync(ticket, cancellationToken);
    void UpdateSupportTicketAsyncById(SupportTicket ticket) => Update(ticket);
    void DeleteSupportTicketAsyncById(SupportTicket ticket) => Delete(ticket);
}
