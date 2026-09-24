using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.TicketTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class TicketTypeService : ITicketTypeService
{
    private readonly ITicketTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TicketTypeService> _logger;

    public TicketTypeService(
        ITicketTypeRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TicketTypeService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<TicketTypeDto>> GetAllTicketTypeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(activeOnly, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<TicketTypeDto>>(entities);
    }

    public async Task<TicketTypeDto?> GetTicketTypeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : _mapper.Map<TicketTypeDto>(entity);
    }

    public async Task<TicketTypeDto> SaveTicketTypeAsync(CreateTicketTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.TypeName.Trim();
        var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Ticket type '{trimmedName}' already exists.");
        }

        var entity = new TicketType
        {
            TicketTypeId = Guid.NewGuid(),
            TypeName = trimmedName,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TicketTypeDto>(entity);
    }

    public async Task<TicketTypeDto> UpdateTicketTypeAsyncById(Guid id, UpdateTicketTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket type with ID {id} was not found.");

        var trimmedName = request.TypeName.Trim();
        var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
        if (existing != null && existing.TicketTypeId != id)
        {
            throw new InvalidOperationException($"Another ticket type '{trimmedName}' already exists.");
        }

        entity.TypeName = trimmedName;
        entity.IsActive = request.IsActive;
        entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
        entity.ModifiedOn = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TicketTypeDto>(entity);
    }

    public async Task DeleteTicketTypeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Ticket type with ID {id} was not found.");

        _repository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
