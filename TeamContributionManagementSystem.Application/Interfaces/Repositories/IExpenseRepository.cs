using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    void Update(Expense expense);
    void Delete(Expense expense);

    // Standardized naming
    Task<List<Expense>> GetAllExpenseAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
    Task<Expense?> GetExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => GetByIdAsync(expenseId, cancellationToken);
    Task SaveExpenseAsync(Expense expense, CancellationToken cancellationToken = default) => AddAsync(expense, cancellationToken);
    void UpdateExpenseAsyncById(Expense expense) => Update(expense);
    void DeleteExpenseAsyncById(Expense expense) => Delete(expense);
}
