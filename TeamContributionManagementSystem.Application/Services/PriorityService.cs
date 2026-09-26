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

    public PriorityService(
        IPriorityRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PriorityService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<PriorityDto>> GetAllPriorityAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _repository.GetAllAsync(activeOnly, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<PriorityDto>>(entities);
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
                throw new InvalidOperationException(string.Format(CommonMessages.Priorities.AlreadyExistsFormat, trimmedName));
            }

            var entity = new Priority
            {
                PriorityId = Guid.NewGuid(),
                PriorityName = trimmedName,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
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
            entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
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
