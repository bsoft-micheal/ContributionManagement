using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

/// <summary>
/// Service interface for managing budget calculations and expense estimations.
/// </summary>
public interface IBudgetCalculationService
{
    /// <summary>
    /// Retrieves all budget calculation records.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of budget calculation DTOs.</returns>
    Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a budget calculation record by its unique identifier.
    /// </summary>
    /// <param name="budgetCalculationId">The unique ID of the budget calculation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching budget calculation DTO, or null if not found.</returns>
    Task<BudgetCalculationDto?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new budget calculation record.
    /// </summary>
    /// <param name="request">The creation payload.</param>
    /// <param name="user">The user performing the creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created budget calculation DTO.</returns>
    Task<BudgetCalculationDto> CreateAsync(CreateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing budget calculation record.
    /// </summary>
    /// <param name="budgetCalculationId">The unique ID of the record to update.</param>
    /// <param name="request">The update payload.</param>
    /// <param name="user">The user performing the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated budget calculation DTO.</returns>
    Task<BudgetCalculationDto> UpdateAsync(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a budget calculation record by its unique identifier.
    /// </summary>
    /// <param name="budgetCalculationId">The unique ID of the record to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Standardized alias method to retrieve all budget calculation records.
    /// </summary>
    Task<IReadOnlyCollection<BudgetCalculationDto>> GetAllBudgetCalculationAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);

    /// <summary>
    /// Standardized alias method to retrieve a budget calculation record by ID.
    /// </summary>
    Task<BudgetCalculationDto?> GetBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => GetByIdAsync(budgetCalculationId, cancellationToken);

    /// <summary>
    /// Standardized alias method to create a new budget calculation record.
    /// </summary>
    Task<BudgetCalculationDto> SaveBudgetCalculationAsync(CreateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);

    /// <summary>
    /// Standardized alias method to update an existing budget calculation record by ID.
    /// </summary>
    Task<BudgetCalculationDto> UpdateBudgetCalculationAsyncById(Guid budgetCalculationId, UpdateBudgetCalculationRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(budgetCalculationId, request, user, cancellationToken);

    /// <summary>
    /// Standardized alias method to delete a budget calculation record by ID.
    /// </summary>
    Task DeleteBudgetCalculationAsyncById(Guid budgetCalculationId, CancellationToken cancellationToken = default) => DeleteAsync(budgetCalculationId, cancellationToken);
}
