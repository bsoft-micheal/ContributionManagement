using TeamContributionManagementSystem.Application.DTOs.EventTypes;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IEventTypeService
{
    Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default);
    Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<EventTypeDto>> GetAllEventTypeAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<EventTypeDto> SaveEventTypeAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<EventTypeDto> UpdateEventTypeAsyncById(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(eventTypeId, request, cancellationToken);
    Task DeleteEventTypeAsyncById(Guid eventTypeId, CancellationToken cancellationToken = default) => DeleteAsync(eventTypeId, cancellationToken);
}
