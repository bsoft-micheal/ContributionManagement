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
    private readonly IUserRepository? _userRepository;

    public EventTypeService(
        ILogger<EventTypeService> logger, 
        IEventTypeRepository eventTypeRepository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IUserRepository? userRepository = null)
    {
        _logger = logger;
        _eventTypeRepository = eventTypeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var eventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
            var dtos = _mapper.Map<List<EventTypeDto>>(eventTypes);

            Dictionary<string, string> userDict = new(StringComparer.OrdinalIgnoreCase);
            string adminFallback = "Admin";

            if (_userRepository != null)
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    foreach (var u in users)
                    {
                        var displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.Username ?? string.Empty);
                        userDict[u.UserId.ToString()] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.Username)) userDict[u.Username!] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.FullName)) userDict[u.FullName!] = displayName;
                    }

                    var firstAdmin = users.FirstOrDefault(u => 
                        string.Equals(u.RoleName, "admin", StringComparison.OrdinalIgnoreCase) ||
                        (u.Roles != null && u.Roles.Any(r => string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase))));
                    adminFallback = !string.IsNullOrWhiteSpace(firstAdmin?.FullName) 
                        ? firstAdmin.FullName 
                        : (!string.IsNullOrWhiteSpace(firstAdmin?.Username) ? firstAdmin.Username : "Admin");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch users for event types creator resolution");
                }
            }

            foreach (var dto in dtos)
            {
                var hasEvents = await _eventTypeRepository.HasEventsAsync(dto.EventTypeId, cancellationToken);
                var hasBc = await _eventTypeRepository.HasBudgetCalculationsAsync(dto.EventTypeName, cancellationToken);
                var hasExpenses = await _eventTypeRepository.HasExpensesAsync(dto.EventTypeName, cancellationToken);
                var hasPhotos = await _eventTypeRepository.HasGalleryPhotosAsync(dto.EventTypeName, cancellationToken);

                dto.IsReferred = hasEvents || hasBc || hasExpenses || hasPhotos;

                if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && userDict.TryGetValue(dto.CreatedBy, out var createdByName))
                {
                    dto.CreatedBy = createdByName;
                }
                else if (string.IsNullOrWhiteSpace(dto.CreatedBy) || dto.CreatedBy == "--" || Guid.TryParse(dto.CreatedBy, out _))
                {
                    dto.CreatedBy = adminFallback;
                }

                if (!string.IsNullOrWhiteSpace(dto.ModifiedBy) && userDict.TryGetValue(dto.ModifiedBy, out var updatedByName))
                {
                    dto.ModifiedBy = updatedByName;
                }
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    private async Task<Guid?> ResolveUserIdAsync(string? user, CancellationToken cancellationToken)
    {
        var guid = CommonMethods.ParseNullableGuid(user);
        if (guid.HasValue) return guid;

        if (!string.IsNullOrWhiteSpace(user) && _userRepository != null)
        {
            var matched = await _userRepository.GetByUsernameOrEmailAsync(user, cancellationToken);
            if (matched != null) return matched.UserId;
        }

        if (_userRepository != null)
        {
            var admin = await _userRepository.GetFirstAdminAsync(cancellationToken);
            if (admin != null) return admin.UserId;
        }

        return null;
    }

    private async Task EnrichDtoUserNamesAsync(EventTypeDto dto, CancellationToken cancellationToken)
    {
        if (_userRepository != null)
        {
            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);
                var userDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var u in users)
                {
                    var displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.Username ?? string.Empty);
                    userDict[u.UserId.ToString()] = displayName;
                    if (!string.IsNullOrWhiteSpace(u.Username)) userDict[u.Username!] = displayName;
                    if (!string.IsNullOrWhiteSpace(u.FullName)) userDict[u.FullName!] = displayName;
                }

                var firstAdmin = users.FirstOrDefault(u => 
                    string.Equals(u.RoleName, "admin", StringComparison.OrdinalIgnoreCase) ||
                    (u.Roles != null && u.Roles.Any(r => string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase))));
                var adminFallback = !string.IsNullOrWhiteSpace(firstAdmin?.FullName) 
                    ? firstAdmin.FullName 
                    : (!string.IsNullOrWhiteSpace(firstAdmin?.Username) ? firstAdmin.Username : "Admin");

                if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && userDict.TryGetValue(dto.CreatedBy, out var createdByName))
                {
                    dto.CreatedBy = createdByName;
                }
                else if (string.IsNullOrWhiteSpace(dto.CreatedBy) || dto.CreatedBy == "--" || Guid.TryParse(dto.CreatedBy, out _))
                {
                    dto.CreatedBy = adminFallback;
                }

                if (!string.IsNullOrWhiteSpace(dto.ModifiedBy) && userDict.TryGetValue(dto.ModifiedBy, out var updatedByName))
                {
                    dto.ModifiedBy = updatedByName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve user names for event type dto");
            }
        }

        if (string.IsNullOrWhiteSpace(dto.CreatedBy) || dto.CreatedBy == "--" || Guid.TryParse(dto.CreatedBy, out _))
        {
            dto.CreatedBy = "Admin";
        }
    }

    public async Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _eventTypeRepository.GetByNameAsync(request.EventTypeName.Trim(), cancellationToken);
            var userId = await ResolveUserIdAsync(user, cancellationToken);

            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = request.IsActive;
                    existing.HasTenureRule = request.HasTenureRule;
                    existing.TenureThresholdYears = request.TenureThresholdYears > 0 ? request.TenureThresholdYears : 1.0m;
                    existing.NewEntrantSharePercentage = request.NewEntrantSharePercentage > 0 ? request.NewEntrantSharePercentage : 50.0m;
                    existing.StandardSharePercentage = request.StandardSharePercentage > 0 ? request.StandardSharePercentage : 100.0m;
                    existing.RuleDescription = request.RuleDescription?.Trim();
                    existing.ModifiedBy = userId;
                    existing.ModifiedOn = DateTime.UtcNow;

                    _eventTypeRepository.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var restoredDto = _mapper.Map<EventTypeDto>(existing);
                    await EnrichDtoUserNamesAsync(restoredDto, cancellationToken);
                    return restoredDto;
                }

                throw new InvalidOperationException(CommonMessages.EventTypes.AlreadyExists);
            }

            var eventType = new EventType
            {
                EventTypeId = Guid.NewGuid(),
                EventTypeName = request.EventTypeName.Trim(),
                IsActive = request.IsActive,
                HasTenureRule = request.HasTenureRule,
                TenureThresholdYears = request.TenureThresholdYears > 0 ? request.TenureThresholdYears : 1.0m,
                NewEntrantSharePercentage = request.NewEntrantSharePercentage > 0 ? request.NewEntrantSharePercentage : 50.0m,
                StandardSharePercentage = request.StandardSharePercentage > 0 ? request.StandardSharePercentage : 100.0m,
                RuleDescription = request.RuleDescription?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _eventTypeRepository.AddAsync(eventType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var createdDto = _mapper.Map<EventTypeDto>(eventType);
            await EnrichDtoUserNamesAsync(createdDto, cancellationToken);
            return createdDto;
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

            var userId = await ResolveUserIdAsync(user, cancellationToken);

            eventType.EventTypeName = request.EventTypeName.Trim();
            eventType.IsActive = request.IsActive;
            eventType.HasTenureRule = request.HasTenureRule;
            eventType.TenureThresholdYears = request.TenureThresholdYears > 0 ? request.TenureThresholdYears : 1.0m;
            eventType.NewEntrantSharePercentage = request.NewEntrantSharePercentage > 0 ? request.NewEntrantSharePercentage : 50.0m;
            eventType.StandardSharePercentage = request.StandardSharePercentage > 0 ? request.StandardSharePercentage : 100.0m;
            eventType.RuleDescription = request.RuleDescription?.Trim();
            eventType.ModifiedBy = userId;
            eventType.ModifiedOn = DateTime.UtcNow;

            _eventTypeRepository.Update(eventType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedDto = _mapper.Map<EventTypeDto>(eventType);
            await EnrichDtoUserNamesAsync(updatedDto, cancellationToken);
            return updatedDto;
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
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

            if (await _eventTypeRepository.HasBudgetCalculationsAsync(eventType.EventTypeName, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

            if (await _eventTypeRepository.HasExpensesAsync(eventType.EventTypeName, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

            if (await _eventTypeRepository.HasGalleryPhotosAsync(eventType.EventTypeName, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
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
