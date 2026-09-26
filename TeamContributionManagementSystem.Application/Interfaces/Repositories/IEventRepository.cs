using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<List<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<List<EventSummaryDto>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDetailsDto?> GetByIdWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> BirthdayEventExistsAsync(Guid memberId, int month, int year, CancellationToken cancellationToken = default);
    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);
    void Update(Event eventItem);
    void DeleteParticipants(IEnumerable<EventParticipant> participants);

    // Standardized naming
    Task<List<EventSummaryDto>> GetAllEventAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetAllAsync(month, year, cancellationToken);
    Task<Event?> GetEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => GetByIdAsync(eventId, cancellationToken);
    Task SaveEventAsync(Event eventItem, CancellationToken cancellationToken = default) => AddAsync(eventItem, cancellationToken);
    void UpdateEventAsyncById(Event eventItem) => Update(eventItem);
}
