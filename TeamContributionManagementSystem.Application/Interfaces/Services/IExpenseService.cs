using TeamContributionManagementSystem.Application.DTOs.Expenses;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IExpenseService
{
    Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto> UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<ExpenseDto>> GetAllExpenseAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        => GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
    Task<ExpenseDto> GetExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => GetByIdAsync(expenseId, cancellationToken);
    Task<ExpenseDto> SaveExpenseAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<ExpenseDto> UpdateExpenseAsyncById(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(expenseId, request, user, cancellationToken);
    Task DeleteExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => DeleteAsync(expenseId, cancellationToken);
}
