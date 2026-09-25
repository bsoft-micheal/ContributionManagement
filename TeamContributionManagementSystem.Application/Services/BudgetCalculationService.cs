using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class BudgetCalculationService : IBudgetCalculationService
{
    private readonly ILogger<BudgetCalculationService> _logger;
    private readonly IBudgetCalculationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BudgetCalculationService(
        ILogger<BudgetCalculationService> logger,
        IBudgetCalculationRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _logger = logger;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await _repository.GetAllAsync(cancellationToken);
            return _mapper.Map<IReadOnlyCollection<BudgetCalculationDto>>(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync for BudgetCalculationService");
            throw;
        }
    }

    public async Task<BudgetCalculationDto?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken);
            return item is not null ? _mapper.Map<BudgetCalculationDto>(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync for BudgetCalculationService");
            throw;
        }
    }

    public async Task<BudgetCalculationDto> CreateAsync(CreateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _repository.GetByNameAsync(request.ExpenseItem.Trim(), cancellationToken);
            if (existing is not null)
            {
                throw new InvalidOperationException($"Expense item '{request.ExpenseItem.Trim()}' already exists.");
            }

            var item = new BudgetCalculation
            {
                BudgetCalculationId = Guid.NewGuid(),
                ExpenseItem = request.ExpenseItem.Trim(),
                Rate = request.Rate,
                Category = request.Category?.Trim() ?? string.Empty,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user,
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BudgetCalculationDto>(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync for BudgetCalculationService");
            throw;
        }
    }

    public async Task<BudgetCalculationDto> UpdateAsync(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException("Budget calculation item not found.");

            var duplicate = await _repository.GetByNameAsync(request.ExpenseItem.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.BudgetCalculationId != budgetCalculationId)
            {
                throw new InvalidOperationException($"Expense item '{request.ExpenseItem.Trim()}' already exists.");
            }

            item.ExpenseItem = request.ExpenseItem.Trim();
            item.Rate = request.Rate;
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                item.Category = request.Category.Trim();
            }
            item.IsActive = request.IsActive;
            item.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user;
            item.ModifiedOn = DateTime.UtcNow;

            _repository.Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BudgetCalculationDto>(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync for BudgetCalculationService");
            throw;
        }
    }

    public async Task DeleteAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException("Budget calculation item not found.");

            _repository.Delete(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync for BudgetCalculationService");
            throw;
        }
    }
}
