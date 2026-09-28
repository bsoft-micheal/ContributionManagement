using TeamContributionManagementSystem.Application.DTOs.TicketTypes;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface ITicketTypeRepository
{
    Task<IReadOnlyCollection<TicketTypeDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TicketType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(TicketType ticketType, CancellationToken cancellationToken = default);
    void Update(TicketType ticketType);
    void Delete(TicketType ticketType);
}
