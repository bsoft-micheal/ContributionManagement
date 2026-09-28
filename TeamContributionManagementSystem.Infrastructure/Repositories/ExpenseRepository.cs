using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExpenseRepository> _logger;

    public ExpenseRepository(ApplicationDbContext context, ILogger<ExpenseRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Expenses.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventName) && !eventName.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.EventName.ToLower() == eventName.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
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

            return await query
                .OrderByDescending(x => x.ExpenseDate)
                .Select(x => new ExpenseDto
                {
                    ExpenseId = x.ExpenseId,
                    EventName = x.EventName,
                    Category = x.Category,
                    Amount = x.Amount,
                    ExpenseDate = x.ExpenseDate,
                    Status = x.Status,
                    SubmittedBy = x.SubmittedBy,
                    ApprovedBy = x.ApprovedBy,
                    Description = x.Description,
                    FileName = x.FileName,
                    FileUrl = x.FileName,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
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

    public async Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Expenses.FirstOrDefaultAsync(x => x.ExpenseId == expenseId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }
}
