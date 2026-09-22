using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<ExpenseRepository> _logger;

    public ExpenseRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<ExpenseRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Expense>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Expenses.Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventName) && eventName != "ALL")
        {
            query = query.Where(x => x.EventName.ToLower() == eventName.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "ALL")
        {
            query = query.Where(x => x.Category.ToLower() == category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            query = query.Where(x => x.Status.ToLower() == status.ToLower());
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.ExpenseDate <= endDate.Value);
        }

        return await query.OrderByDescending(x => x.ExpenseDate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Expenses.FirstOrDefaultAsync(x => x.ExpenseId == expenseId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Expenses.AddAsync(expense, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddAsync");
            throw;
        }
    }

    public void Update(Expense expense)
    {
        try
        {
            _context.Expenses.Update(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            throw;
        }
    }

    public void Delete(Expense expense)
    {
        try
        {
            expense.IsDeleted = true;
        expense.ModifiedOn = DateTime.UtcNow;
        _context.Expenses.Update(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            throw;
        }
    }
}
