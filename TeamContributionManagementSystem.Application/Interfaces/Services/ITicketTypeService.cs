using TeamContributionManagementSystem.Application.DTOs.TicketTypes;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface ITicketTypeService
{
    Task<IReadOnlyCollection<TicketTypeDto>> GetAllTicketTypeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<TicketTypeDto?> GetTicketTypeAsyncById(Guid id, CancellationToken cancellationToken = default);
    Task<TicketTypeDto> SaveTicketTypeAsync(CreateTicketTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<TicketTypeDto> UpdateTicketTypeAsyncById(Guid id, UpdateTicketTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteTicketTypeAsyncById(Guid id, CancellationToken cancellationToken = default);
}
