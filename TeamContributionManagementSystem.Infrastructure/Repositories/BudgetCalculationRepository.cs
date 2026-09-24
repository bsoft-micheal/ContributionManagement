using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public async Task<List<BudgetCalculation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.BudgetCalculations
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync for BudgetCalculation");
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
            _logger.LogError(ex, "Error in GetByIdAsync for BudgetCalculation");
            throw;
        }
    }

    public async Task<BudgetCalculation?> GetByNameAsync(string expenseItem, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.BudgetCalculations
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.ExpenseItem.ToLower() == expenseItem.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByNameAsync for BudgetCalculation");
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
            _logger.LogError(ex, "Error in AddAsync for BudgetCalculation");
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
            _logger.LogError(ex, "Error in Update for BudgetCalculation");
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
            _logger.LogError(ex, "Error in Delete for BudgetCalculation");
            throw;
        }
    }
}
