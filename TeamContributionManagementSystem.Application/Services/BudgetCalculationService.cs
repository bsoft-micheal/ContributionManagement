using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
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
                throw new InvalidOperationException(string.Format(CommonMessages.BudgetCalculations.AlreadyExistsFormat, request.ExpenseItem.Trim()));
            }

            var item = new BudgetCalculation
            {
                BudgetCalculationId = Guid.NewGuid(),
                ExpenseItem = request.ExpenseItem.Trim(),
                Rate = request.Rate,
                Category = request.Category?.Trim() ?? string.Empty,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BudgetCalculationDto>(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<BudgetCalculationDto> UpdateAsync(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            var duplicate = await _repository.GetByNameAsync(request.ExpenseItem.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.BudgetCalculationId != budgetCalculationId)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.BudgetCalculations.AlreadyExistsFormat, request.ExpenseItem.Trim()));
            }

            item.ExpenseItem = request.ExpenseItem.Trim();
            item.Rate = request.Rate;
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                item.Category = request.Category.Trim();
            }
            item.IsActive = request.IsActive;
            item.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            item.ModifiedOn = DateTime.UtcNow;

            _repository.Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BudgetCalculationDto>(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            _repository.Delete(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
