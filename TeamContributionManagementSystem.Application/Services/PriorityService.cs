using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Priorities;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class PriorityService : IPriorityService
{
    private readonly IPriorityRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PriorityService> _logger;
    private readonly IUserRepository? _userRepository;

    public PriorityService(
        IPriorityRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PriorityService> logger,
        IUserRepository? userRepository = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<PriorityDto>> GetAllPriorityAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _repository.GetAllAsync(activeOnly, cancellationToken);
            var dtos = _mapper.Map<List<PriorityDto>>(entities);

            if (_userRepository != null)
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    var userDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var u in users)
                    {
                        var displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
                        userDict[u.UserId.ToString()] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.Username)) userDict[u.Username] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.FullName)) userDict[u.FullName] = displayName;
                    }

                    foreach (var dto in dtos)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && userDict.TryGetValue(dto.CreatedBy, out var createdByName))
                        {
                            dto.CreatedBy = createdByName;
                        }
                        if (!string.IsNullOrWhiteSpace(dto.ModifiedBy) && userDict.TryGetValue(dto.ModifiedBy, out var updatedByName))
                        {
                            dto.ModifiedBy = updatedByName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve user names for priorities");
                }
            }

            foreach (var dto in dtos)
            {
                dto.IsReferred = await _repository.HasSupportTicketsAsync(dto.PriorityName, cancellationToken);
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllPriorityAsync));
            throw;
        }
    }

    public async Task<PriorityDto?> GetPriorityAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity == null ? null : _mapper.Map<PriorityDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetPriorityAsyncById));
            throw;
        }
    }

    public async Task<PriorityDto> SavePriorityAsync(CreatePriorityRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedName = request.PriorityName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = request.IsActive;
                    existing.ModifiedBy = CommonMethods.ParseNullableGuid(user);
                    existing.ModifiedOn = DateTime.UtcNow;

                    _repository.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return _mapper.Map<PriorityDto>(existing);
                }

                throw new InvalidOperationException(string.Format(CommonMessages.Priorities.AlreadyExistsFormat, trimmedName));
            }

            var entity = new Priority
            {
                PriorityId = Guid.NewGuid(),
                PriorityName = trimmedName,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PriorityDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(SavePriorityAsync));
            throw;
        }
    }

    public async Task<PriorityDto> UpdatePriorityAsyncById(Guid id, UpdatePriorityRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.Priorities.NotFoundFormat, id));

            var trimmedName = request.PriorityName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            if (existing != null && existing.PriorityId != id)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.Priorities.AlreadyExistsFormat, trimmedName));
            }

            entity.PriorityName = trimmedName;
            entity.IsActive = request.IsActive;
            entity.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            entity.ModifiedOn = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PriorityDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdatePriorityAsyncById));
            throw;
        }
    }

    public async Task DeletePriorityAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.Priorities.NotFoundFormat, id));

            if (await _repository.HasSupportTicketsAsync(entity.PriorityName, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeletePriorityAsyncById));
            throw;
        }
    }
}
