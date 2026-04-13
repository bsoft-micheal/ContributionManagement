using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class EventTypeService : IEventTypeService
{
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EventTypeService(IEventTypeRepository eventTypeRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _eventTypeRepository = eventTypeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var eventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<EventTypeDto>>(eventTypes);
    }

    public async Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _eventTypeRepository.GetByNameAsync(request.EventTypeName.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Event type already exists.");
        }

        var eventType = new EventType
        {
            EventTypeId = Guid.NewGuid(),
            EventTypeName = request.EventTypeName.Trim(),
            IsActive = request.IsActive
        };

        await _eventTypeRepository.AddAsync(eventType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EventTypeDto>(eventType);
    }

    public async Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Event type not found.");

        var duplicate = await _eventTypeRepository.GetByNameAsync(request.EventTypeName.Trim(), cancellationToken);
        if (duplicate is not null && duplicate.EventTypeId != eventTypeId)
        {
            throw new InvalidOperationException("Event type already exists.");
        }

        eventType.EventTypeName = request.EventTypeName.Trim();
        eventType.IsActive = request.IsActive;

        _eventTypeRepository.Update(eventType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EventTypeDto>(eventType);
    }
}
