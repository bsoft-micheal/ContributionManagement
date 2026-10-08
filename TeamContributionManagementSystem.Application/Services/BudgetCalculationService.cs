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
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BudgetCalculationService(
        ILogger<BudgetCalculationService> logger,
        IBudgetCalculationRepository repository,
        IEventTypeRepository eventTypeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _logger = logger;
        _repository = repository;
        _eventTypeRepository = eventTypeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetAllAsync(cancellationToken);
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
            if (request.Rate <= 0)
            {
                throw new ArgumentException(CommonValidationMessages.RateRange);
            }

            var existing = await _repository.GetByNameAsync(request.ExpenseItem.Trim(), request.Category?.Trim(), cancellationToken);
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = request.IsActive;
                    existing.Rate = request.Rate;
                    if (request.EventTypeId.HasValue) existing.EventTypeId = request.EventTypeId.Value;
                    if (!string.IsNullOrWhiteSpace(request.Category)) existing.Category = request.Category.Trim();
                    existing.ModifiedBy = CommonMethods.ParseNullableGuid(user);
                    existing.ModifiedOn = DateTime.UtcNow;

                    _repository.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return _mapper.Map<BudgetCalculationDto>(existing);
                }

                throw new InvalidOperationException(string.Format(CommonMessages.BudgetCalculations.AlreadyExistsFormat, request.ExpenseItem.Trim()));
            }

            Guid? eventTypeId = request.EventTypeId;
            string? categoryName = request.Category?.Trim();

            if (!eventTypeId.HasValue && !string.IsNullOrWhiteSpace(categoryName))
            {
                var eventType = await _eventTypeRepository.GetByNameAsync(categoryName, cancellationToken);
                if (eventType != null)
                {
                    eventTypeId = eventType.EventTypeId;
                    categoryName = eventType.EventTypeName;
                }
            }
            else if (eventTypeId.HasValue && string.IsNullOrWhiteSpace(categoryName))
            {
                var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId.Value, cancellationToken);
                if (eventType != null)
                {
                    categoryName = eventType.EventTypeName;
                }
            }

            var item = new BudgetCalculation
            {
                BudgetCalculationId = Guid.NewGuid(),
                EventTypeId = eventTypeId,
                ExpenseItem = request.ExpenseItem.Trim(),
                Rate = request.Rate,
                Category = categoryName,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(item, cancellationToken);

            var initialHistory = new BudgetCalculationHistory
            {
                HistoryId = Guid.NewGuid(),
                BudgetCalculationId = item.BudgetCalculationId,
                PreviousRate = null,
                NewRate = item.Rate,
                EffectiveFrom = DateTime.UtcNow.Date,
                ChangeType = "INITIAL",
                ChangeAmount = 0,
                ChangePercentage = 0,
                ChangeReason = "Initial rate",
                Remarks = null,
                ChangedBy = item.CreatedBy,
                ChangedOn = DateTime.UtcNow,
                IsActive = true
            };
            await _repository.AddHistoryAsync(initialHistory, cancellationToken);

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
            if (request.Rate < 0)
            {
                throw new ArgumentException(CommonValidationMessages.RateRange);
            }

            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            var duplicate = await _repository.GetByNameAsync(request.ExpenseItem.Trim(), request.Category?.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.BudgetCalculationId != budgetCalculationId)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.BudgetCalculations.AlreadyExistsFormat, request.ExpenseItem.Trim()));
            }

            Guid? eventTypeId = request.EventTypeId;
            string? categoryName = request.Category?.Trim();

            if (!eventTypeId.HasValue && !string.IsNullOrWhiteSpace(categoryName))
            {
                var eventType = await _eventTypeRepository.GetByNameAsync(categoryName, cancellationToken);
                if (eventType != null)
                {
                    eventTypeId = eventType.EventTypeId;
                    categoryName = eventType.EventTypeName;
                }
            }
            else if (eventTypeId.HasValue && string.IsNullOrWhiteSpace(categoryName))
            {
                var eventType = await _eventTypeRepository.GetByIdAsync(eventTypeId.Value, cancellationToken);
                if (eventType != null)
                {
                    categoryName = eventType.EventTypeName;
                }
            }

            decimal previousRate = item.Rate;
            decimal newRate = request.Rate;

            // If rate changed via generic update form, track history
            if (newRate != previousRate)
            {
                string changeReason = !string.IsNullOrWhiteSpace(request.ChangeReason)
                    ? request.ChangeReason.Trim()
                    : (newRate > previousRate ? "Rate increased" : "Rate decreased");

                decimal changeAmount = newRate - previousRate;
                decimal changePercentage = previousRate != 0 ? ((newRate - previousRate) / previousRate) * 100m : 0m;
                string changeType = newRate > previousRate ? "INCREASE" : "DECREASE";

                var history = new BudgetCalculationHistory
                {
                    HistoryId = Guid.NewGuid(),
                    BudgetCalculationId = item.BudgetCalculationId,
                    PreviousRate = previousRate,
                    NewRate = newRate,
                    EffectiveFrom = request.EffectiveFrom.HasValue && request.EffectiveFrom.Value != default
                        ? request.EffectiveFrom.Value.Date
                        : DateTime.UtcNow.Date,
                    ChangeType = changeType,
                    ChangeAmount = changeAmount,
                    ChangePercentage = Math.Round(changePercentage, 2),
                    ChangeReason = changeReason,
                    Remarks = !string.IsNullOrWhiteSpace(request.Remarks) ? request.Remarks.Trim() : null,
                    ChangedBy = CommonMethods.ParseNullableGuid(user),
                    ChangedOn = DateTime.UtcNow,
                    IsActive = true
                };

                await _repository.AddHistoryAsync(history, cancellationToken);
            }

            item.ExpenseItem = request.ExpenseItem.Trim();
            item.Rate = request.Rate;
            if (eventTypeId.HasValue)
            {
                item.EventTypeId = eventTypeId;
            }
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                item.Category = categoryName;
            }
            item.IsActive = request.IsActive;
            item.ModifiedBy = CommonMethods.ParseNullableGuid(user);
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

    public async Task<BudgetCalculationDto> UpdateRateAsync(Guid budgetCalculationId, UpdateBudgetCalculationRateRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.NewRate < 0)
            {
                throw new ArgumentException("New rate must be greater than or equal to 0.");
            }

            if (string.IsNullOrWhiteSpace(request.ChangeReason) || request.ChangeReason.Trim().Length < 5)
            {
                throw new ArgumentException("Please enter the reason for the rate change (minimum 5 characters).");
            }

            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            decimal previousRate = item.Rate;
            decimal newRate = request.NewRate;

            if (newRate == previousRate)
            {
                throw new InvalidOperationException("New rate must be different from the current rate.");
            }

            decimal changeAmount = newRate - previousRate;
            decimal changePercentage = previousRate != 0 ? ((newRate - previousRate) / previousRate) * 100m : 0m;
            string changeType = newRate > previousRate ? "INCREASE" : "DECREASE";

            var history = new BudgetCalculationHistory
            {
                HistoryId = Guid.NewGuid(),
                BudgetCalculationId = item.BudgetCalculationId,
                PreviousRate = previousRate,
                NewRate = newRate,
                EffectiveFrom = request.EffectiveFrom != default ? request.EffectiveFrom.Date : DateTime.UtcNow.Date,
                ChangeType = changeType,
                ChangeAmount = changeAmount,
                ChangePercentage = Math.Round(changePercentage, 2),
                ChangeReason = request.ChangeReason.Trim(),
                Remarks = !string.IsNullOrWhiteSpace(request.Remarks) ? request.Remarks.Trim() : null,
                ChangedBy = CommonMethods.ParseNullableGuid(user),
                ChangedOn = DateTime.UtcNow,
                IsActive = true
            };

            item.Rate = newRate;
            item.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            item.ModifiedOn = DateTime.UtcNow;

            await _repository.AddHistoryAsync(history, cancellationToken);
            _repository.Update(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<BudgetCalculationDto>(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateRateAsync));
            throw;
        }
    }

    public async Task<IReadOnlyCollection<BudgetCalculationHistoryDto>> GetHistoryAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            return await _repository.GetHistoryByBudgetIdAsync(budgetCalculationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetHistoryAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _repository.GetByIdAsync(budgetCalculationId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.BudgetCalculations.NotFound);

            if (await _repository.HasExpensesAsync(item.ExpenseItem, item.Category, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

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
