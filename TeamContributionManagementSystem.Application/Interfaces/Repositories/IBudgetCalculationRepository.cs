using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IBudgetCalculationRepository
{
    Task<List<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetCalculation?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);
    Task<BudgetCalculation?> GetByNameAsync(string expenseItem, string? category = null, CancellationToken cancellationToken = default);
    Task<bool> HasExpensesAsync(string expenseItem, string? category = null, CancellationToken cancellationToken = default);
    Task<List<BudgetCalculation>> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task AddAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default);
    void Update(BudgetCalculation budgetCalculation);
    void Delete(BudgetCalculation budgetCalculation);

    Task<List<BudgetCalculationHistoryDto>> GetHistoryByBudgetIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(BudgetCalculationHistory history, CancellationToken cancellationToken = default);
    Task<BudgetCalculationHistory?> GetLatestHistoryAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);

    // Standardized aliases
    Task<List<BudgetCalculationDto>> GetAllBudgetCalculationAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<BudgetCalculation?> GetBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => GetByIdAsync(budgetCalculationId, cancellationToken);
    Task SaveBudgetCalculationAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default) => AddAsync(budgetCalculation, cancellationToken);
    void UpdateBudgetCalculationAsyncById(BudgetCalculation budgetCalculation) => Update(budgetCalculation);
    void DeleteBudgetCalculationAsyncById(BudgetCalculation budgetCalculation) => Delete(budgetCalculation);
    Task<List<BudgetCalculationHistoryDto>> GetBudgetCalculationHistoryAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => GetHistoryByBudgetIdAsync(budgetCalculationId, cancellationToken);
}
