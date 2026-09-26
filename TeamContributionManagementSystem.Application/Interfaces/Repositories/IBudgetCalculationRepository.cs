using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IBudgetCalculationRepository
{
    Task<List<BudgetCalculation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BudgetCalculation?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);
    Task<BudgetCalculation?> GetByNameAsync(string expenseItem, string? category = null, CancellationToken cancellationToken = default);
    Task AddAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default);
    void Update(BudgetCalculation budgetCalculation);
    void Delete(BudgetCalculation budgetCalculation);

    // Standardized aliases
    Task<List<BudgetCalculation>> GetAllBudgetCalculationAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<BudgetCalculation?> GetBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => GetByIdAsync(budgetCalculationId, cancellationToken);
    Task SaveBudgetCalculationAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default) => AddAsync(budgetCalculation, cancellationToken);
    void UpdateBudgetCalculationAsyncById(BudgetCalculation budgetCalculation) => Update(budgetCalculation);
    void DeleteBudgetCalculationAsyncById(BudgetCalculation budgetCalculation) => Delete(budgetCalculation);
}
