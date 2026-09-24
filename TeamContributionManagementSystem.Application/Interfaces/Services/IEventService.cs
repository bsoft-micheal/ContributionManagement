using TeamContributionManagementSystem.Application.DTOs.Events;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IEventService
{
    Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> CreateAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<EventSummaryDto>> GetAllEventAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetAllAsync(month, year, cancellationToken);
    Task<EventDetailsDto> GetEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => GetByIdAsync(eventId, cancellationToken);
    Task<EventDetailsDto> SaveEventAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(createdByUserId, request, cancellationToken);
    Task<EventDetailsDto> UpdateEventAsyncById(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(eventId, request, cancellationToken);
    Task DeleteEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => DeleteAsync(eventId, cancellationToken);
}
