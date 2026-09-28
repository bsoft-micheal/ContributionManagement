using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class BudgetCalculationRepository : IBudgetCalculationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BudgetCalculationRepository> _logger;

    public BudgetCalculationRepository(ApplicationDbContext context, ILogger<BudgetCalculationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.BudgetCalculations
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new BudgetCalculationDto
                {
                    BudgetCalculationId = x.BudgetCalculationId,
                    ExpenseItem = x.ExpenseItem,
                    Rate = x.Rate,
                    Category = x.Category,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<BudgetCalculation?> GetByIdAsync(Guid budgetCalculationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.BudgetCalculations
                .FirstOrDefaultAsync(x => x.BudgetCalculationId == budgetCalculationId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<BudgetCalculation?> GetByNameAsync(string expenseItem, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.BudgetCalculations
                .Where(x => !x.IsDeleted && x.ExpenseItem.ToLower() == expenseItem.ToLower());

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category != null && x.Category.ToLower() == category.ToLower());
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByNameAsync));
            throw;
        }
    }

    public async Task AddAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.BudgetCalculations.AddAsync(budgetCalculation, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(BudgetCalculation budgetCalculation)
    {
        try
        {
            _context.BudgetCalculations.Update(budgetCalculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(BudgetCalculation budgetCalculation)
    {
        try
        {
            budgetCalculation.IsDeleted = true;
            budgetCalculation.ModifiedOn = DateTime.UtcNow;
            _context.BudgetCalculations.Update(budgetCalculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }
}
