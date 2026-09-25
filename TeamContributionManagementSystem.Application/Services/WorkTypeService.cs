using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.WorkTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class WorkTypeService : IWorkTypeService
{
    private readonly IWorkTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<WorkTypeService> _logger;

    public WorkTypeService(
        IWorkTypeRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<WorkTypeService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<WorkTypeDto>> GetAllWorkTypeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(activeOnly, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<WorkTypeDto>>(entities);
    }

    public async Task<WorkTypeDto?> GetWorkTypeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : _mapper.Map<WorkTypeDto>(entity);
    }

    public async Task<WorkTypeDto> SaveWorkTypeAsync(CreateWorkTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.WorkTypeName.Trim();
        var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Work type '{trimmedName}' already exists.");
        }

        var entity = new WorkType
        {
            WorkTypeId = Guid.NewGuid(),
            WorkTypeName = trimmedName,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<WorkTypeDto>(entity);
    }

    public async Task<WorkTypeDto> UpdateWorkTypeAsyncById(Guid id, UpdateWorkTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Work type with ID {id} was not found.");

        var trimmedName = request.WorkTypeName.Trim();
        var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing != null && existing.WorkTypeId != id)
        {
            throw new InvalidOperationException($"Another work type '{trimmedName}' already exists.");
        }

        entity.WorkTypeName = trimmedName;
        entity.IsActive = request.IsActive;
        entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
        entity.ModifiedOn = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<WorkTypeDto>(entity);
    }

    public async Task DeleteWorkTypeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Work type with ID {id} was not found.");

        _repository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
