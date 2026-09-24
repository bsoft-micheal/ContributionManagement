using TeamContributionManagementSystem.Application.DTOs.SupportTickets;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface ISupportTicketService
{
    Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<SupportTicketDto>> GetAllSupportTicketAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
        => GetAllAsync(status, ticketType, priority, cancellationToken);
    Task<SupportTicketDto> GetSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => GetByIdAsync(ticketId, cancellationToken);
    Task<SupportTicketDto> SaveSupportTicketAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<SupportTicketDto> UpdateSupportTicketAsyncById(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(ticketId, request, user, cancellationToken);
    Task<SupportTicketDto> ReplySupportTicketAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => ReplyAsync(ticketId, request, user, cancellationToken);
    Task DeleteSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => DeleteAsync(ticketId, cancellationToken);
}
