using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class EventTypeService : IEventTypeService
{
    private readonly ILogger<EventTypeService> _logger;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EventTypeService(ILogger<EventTypeService> logger, IEventTypeRepository eventTypeRepository, IUnitOfWork unitOfWork, IMapper mapper)
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
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
                throw new InvalidOperationException(CommonMessages.EventTypes.AlreadyExists);
            }

            var eventType = new EventType
            {
                EventTypeId = Guid.NewGuid(),
                EventTypeName = request.EventTypeName.Trim(),
                IsActive = request.IsActive,
                BaseAmount = request.BaseAmount,
                HasTenureRule = request.HasTenureRule,
                TenureThresholdYears = request.TenureThresholdYears > 0 ? request.TenureThresholdYears : 1.0m,
                NewEntrantSharePercentage = request.NewEntrantSharePercentage > 0 ? request.NewEntrantSharePercentage : 50.0m,
                StandardSharePercentage = request.StandardSharePercentage > 0 ? request.StandardSharePercentage : 100.0m,
                RuleDescription = request.RuleDescription?.Trim(),
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _eventTypeRepository.AddAsync(eventType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<EventTypeDto>(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            var duplicate = await _eventTypeRepository.GetByNameAsync(request.EventTypeName.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.EventTypeId != eventTypeId)
            {
                throw new InvalidOperationException(CommonMessages.EventTypes.AlreadyExists);
            }

            eventType.EventTypeName = request.EventTypeName.Trim();
            eventType.IsActive = request.IsActive;
            eventType.BaseAmount = request.BaseAmount;
            eventType.HasTenureRule = request.HasTenureRule;
            eventType.TenureThresholdYears = request.TenureThresholdYears > 0 ? request.TenureThresholdYears : 1.0m;
            eventType.NewEntrantSharePercentage = request.NewEntrantSharePercentage > 0 ? request.NewEntrantSharePercentage : 50.0m;
            eventType.StandardSharePercentage = request.StandardSharePercentage > 0 ? request.StandardSharePercentage : 100.0m;
            eventType.RuleDescription = request.RuleDescription?.Trim();
            eventType.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            eventType.ModifiedOn = DateTime.UtcNow;

            _eventTypeRepository.Update(eventType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<EventTypeDto>(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            if (await _eventTypeRepository.HasEventsAsync(eventTypeId, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.EventTypes.CannotDeleteWithEvents);
            }

            _eventTypeRepository.Delete(eventType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
