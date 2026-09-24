using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IBudgetCalculationService
{
    Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetCalculationDto?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);
    Task<BudgetCalculationDto> CreateAsync(CreateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<BudgetCalculationDto> UpdateAsync(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllBudgetCalculationAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<BudgetCalculationDto?> GetBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => GetByIdAsync(budgetCalculationId, cancellationToken);
    Task<BudgetCalculationDto> SaveBudgetCalculationAsync(CreateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<BudgetCalculationDto> UpdateBudgetCalculationAsyncById(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(budgetCalculationId, request, user, cancellationToken);
    Task DeleteBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => DeleteAsync(budgetCalculationId, cancellationToken);
}
