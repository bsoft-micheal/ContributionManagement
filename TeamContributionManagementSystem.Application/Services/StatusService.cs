using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<StatusService> _logger;

    public StatusService(
        IStatusRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<StatusService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllStatusAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(activeOnly, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllStatusAsync));
            throw;
        }
    }

    public async Task<StatusDto?> GetStatusAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity == null ? null : _mapper.Map<StatusDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetStatusAsyncById));
            throw;
        }
    }

    public async Task<StatusDto> SaveStatusAsync(CreateStatusRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedName = request.StatusName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.Statuses.AlreadyExistsFormat, trimmedName));
            }

            var entity = new Status
            {
                StatusId = Guid.NewGuid(),
                StatusName = trimmedName,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StatusDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(SaveStatusAsync));
            throw;
        }
    }

    public async Task<StatusDto> UpdateStatusAsyncById(Guid id, UpdateStatusRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.Statuses.NotFoundFormat, id));

            var trimmedName = request.StatusName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            if (existing != null && existing.StatusId != id)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.Statuses.AlreadyExistsFormat, trimmedName));
            }

            entity.StatusName = trimmedName;
            entity.IsActive = request.IsActive;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            entity.ModifiedOn = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StatusDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateStatusAsyncById));
            throw;
        }
    }

    public async Task DeleteStatusAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.Statuses.NotFoundFormat, id));

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteStatusAsyncById));
            throw;
        }
    }
}
