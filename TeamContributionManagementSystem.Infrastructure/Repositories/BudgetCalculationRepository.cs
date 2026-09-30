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
            var expenses = await _context.Expenses
                .Where(x => !x.IsDeleted)
                .Select(x => new { Desc = x.Description.ToLower(), Cat = x.Category.ToLower(), Event = x.EventName.ToLower() })
                .ToListAsync(cancellationToken);

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.BudgetCalculations
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new BudgetCalculationDto
                {
                    BudgetCalculationId = x.BudgetCalculationId,
                    ExpenseItem = x.ExpenseItem,
                    Rate = x.Rate,
                    Category = x.EventType != null ? x.EventType.EventTypeName : null,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var itemClean = item.ExpenseItem.Trim().ToLower();
                var catClean = item.Category?.Trim().ToLower();

                item.IsReferred = expenses.Any(x => x.Desc.Contains(itemClean) || (catClean != null && x.Cat == catClean && x.Event.Contains(itemClean)));

                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
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
                var cleanCat = category.Trim().ToLower();
                query = query.Where(x => x.EventType != null && x.EventType.EventTypeName.ToLower() == cleanCat);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByNameAsync));
            throw;
        }
    }

    public async Task<bool> HasExpensesAsync(string expenseItem, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var itemClean = expenseItem.Trim().ToLower();
            var catClean = category?.Trim().ToLower();

            var query = _context.Expenses.Where(x => !x.IsDeleted && 
                (x.Description.ToLower().Contains(itemClean) || 
                 (catClean != null && x.Category.ToLower() == catClean && x.EventName.ToLower().Contains(itemClean))));

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasExpensesAsync));
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
