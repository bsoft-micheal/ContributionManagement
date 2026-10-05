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

    public async Task<IReadOnlyCollection<StatusDto>> GetAllStatusAsync(bool? activeOnly = null, string? module = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(activeOnly, module, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllStatusAsync));
            throw;
        }
    }

    public Task<IReadOnlyCollection<StatusDto>> GetAllStatusAsync(bool? activeOnly, CancellationToken cancellationToken)
    {
        return GetAllStatusAsync(activeOnly, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetModulesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetModulesAsync));
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
            var trimmedModule = string.IsNullOrWhiteSpace(request.Module) ? "General" : request.Module.Trim();
            var existing = await _repository.GetByNameAndModuleAsync(trimmedName, trimmedModule, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"Status '{trimmedName}' already exists for module '{trimmedModule}'.");
            }

            var entity = new Status
            {
                StatusId = Guid.NewGuid(),
                StatusName = trimmedName,
                Module = trimmedModule,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
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
            var trimmedModule = string.IsNullOrWhiteSpace(request.Module) ? (entity.Module ?? "General") : request.Module.Trim();
            var existing = await _repository.GetByNameAndModuleAsync(trimmedName, trimmedModule, cancellationToken);
            if (existing != null && existing.StatusId != id)
            {
                throw new InvalidOperationException($"Status '{trimmedName}' already exists for module '{trimmedModule}'.");
            }

            entity.StatusName = trimmedName;
            entity.Module = trimmedModule;
            entity.IsActive = request.IsActive;
            entity.ModifiedBy = CommonMethods.ParseNullableGuid(user);
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

            if (await _repository.IsInUseAsync(entity.StatusName, entity.Module, cancellationToken))
            {
                throw new InvalidOperationException($"Cannot delete status '{entity.StatusName}' because it is currently assigned to existing records in the system.");
            }

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
