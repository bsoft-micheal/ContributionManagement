using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class EventTypeService : IEventTypeService
{
    private readonly Microsoft.Extensions.Logging.ILogger<EventTypeService> _logger;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EventTypeService(Microsoft.Extensions.Logging.ILogger<EventTypeService> logger, IEventTypeRepository eventTypeRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _eventTypeRepository = eventTypeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var eventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<EventTypeDto>>(eventTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
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
            IsActive = request.IsActive,
            BaseAmount = request.BaseAmount,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user,
            CreatedAt = DateTime.UtcNow
        };

        await _eventTypeRepository.AddAsync(eventType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EventTypeDto>(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
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
        eventType.BaseAmount = request.BaseAmount;
        eventType.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user;
        eventType.ModifiedOn = DateTime.UtcNow;

        _eventTypeRepository.Update(eventType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<EventTypeDto>(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Event type not found.");

        if (await _eventTypeRepository.HasEventsAsync(eventTypeId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete this event type because it is associated with existing events.");
        }

        _eventTypeRepository.Delete(eventType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}
