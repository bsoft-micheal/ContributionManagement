using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IEventTypeRepository
{
    Task<List<EventType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventType?> GetByIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task<EventType?> GetByNameAsync(string eventTypeName, CancellationToken cancellationToken = default);
    Task<bool> HasEventsAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task AddAsync(EventType eventType, CancellationToken cancellationToken = default);
    void Update(EventType eventType);
    void Delete(EventType eventType);

    // Standardized naming
    Task<List<EventType>> GetAllEventTypeAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<EventType?> GetEventTypeAsyncById(Guid eventTypeId, CancellationToken cancellationToken = default) => GetByIdAsync(eventTypeId, cancellationToken);
    Task SaveEventTypeAsync(EventType eventType, CancellationToken cancellationToken = default) => AddAsync(eventType, cancellationToken);
    void UpdateEventTypeAsyncById(EventType eventType) => Update(eventType);
    void DeleteEventTypeAsyncById(EventType eventType) => Delete(eventType);
}
