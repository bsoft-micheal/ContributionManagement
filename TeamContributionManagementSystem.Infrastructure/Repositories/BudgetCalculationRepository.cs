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

    private static bool _columnsEnsured = false;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private async Task EnsureColumnsAsync(CancellationToken cancellationToken)
    {
        if (_columnsEnsured) return;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_columnsEnsured) return;
            await _context.Database.ExecuteSqlRawAsync(@"
                ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS category VARCHAR(100) NULL;
                ALTER TABLE IF EXISTS budget_calculations ADD COLUMN IF NOT EXISTS event_type_id UUID NULL;
            ", cancellationToken);
            _columnsEnsured = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure columns on budget_calculations");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<BudgetCalculationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureColumnsAsync(cancellationToken);
            // Self-heal orphan items missing EventTypeId (e.g. newly created items before fix or legacy items)
            var orphans = await _context.BudgetCalculations
                .Where(x => !x.IsDeleted && x.EventTypeId == null)
                .ToListAsync(cancellationToken);

            if (orphans.Count > 0)
            {
                var allTypes = await _context.EventTypes.ToListAsync(cancellationToken);
                var farewellType = allTypes.FirstOrDefault(t => t.EventTypeName.ToLower() == "farewell");
                var birthdayType = allTypes.FirstOrDefault(t => t.EventTypeName.ToLower() == "birthday");
                bool changed = false;

                foreach (var orphan in orphans)
                {
                    if (orphan.ExpenseItem.Trim().ToLower().Contains("chicken rice") && farewellType != null)
                    {
                        orphan.EventTypeId = farewellType.EventTypeId;
                        orphan.Category = farewellType.EventTypeName;
                        changed = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(orphan.Category))
                    {
                        var matched = allTypes.FirstOrDefault(t => t.EventTypeName.ToLower() == orphan.Category.Trim().ToLower());
                        if (matched != null)
                        {
                            orphan.EventTypeId = matched.EventTypeId;
                            orphan.Category = matched.EventTypeName;
                            changed = true;
                        }
                        else if (birthdayType != null)
                        {
                            orphan.EventTypeId = birthdayType.EventTypeId;
                            orphan.Category = birthdayType.EventTypeName;
                            changed = true;
                        }
                    }
                    else if (birthdayType != null)
                    {
                        orphan.EventTypeId = birthdayType.EventTypeId;
                        orphan.Category = birthdayType.EventTypeName;
                        changed = true;
                    }
                }

                if (changed)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var expenses = await _context.Expenses
                .Where(x => !x.IsDeleted)
                .Select(x => new { Desc = x.Description.ToLower(), Cat = x.Category.ToLower(), Event = x.EventName.ToLower() })
                .ToListAsync(cancellationToken);

            var events = await _context.Events
                .Where(x => !x.IsDeleted)
                .Select(x => new
                {
                    x.EventTypeId,
                    EventTypeName = x.EventType != null ? x.EventType.EventTypeName.ToLower() : "",
                    EventName = x.EventName.ToLower()
                })
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
                    EventTypeId = x.EventTypeId,
                    ExpenseItem = x.ExpenseItem,
                    Rate = x.Rate,
                    Category = x.EventType != null ? x.EventType.EventTypeName : (!string.IsNullOrWhiteSpace(x.Category) ? x.Category : null),
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

                bool isUsedInExpenses = expenses.Any(x => x.Desc.Contains(itemClean) || (catClean != null && x.Cat == catClean && x.Event.Contains(itemClean)));

                item.IsReferred = isUsedInExpenses;

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
            await EnsureColumnsAsync(cancellationToken);
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
            await EnsureColumnsAsync(cancellationToken);
            var query = _context.BudgetCalculations
                .Where(x => !x.IsDeleted && x.ExpenseItem.ToLower() == expenseItem.ToLower());

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cleanCat = category.Trim().ToLower();
                query = query.Where(x => (x.EventType != null && x.EventType.EventTypeName.ToLower() == cleanCat) ||
                                         (x.Category != null && x.Category.ToLower() == cleanCat));
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

    public async Task<List<BudgetCalculation>> GetByEventTypeIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureColumnsAsync(cancellationToken);
            var eventType = await _context.EventTypes.FirstOrDefaultAsync(t => t.EventTypeId == eventTypeId, cancellationToken);
            var typeName = eventType?.EventTypeName.ToLower();

            return await _context.BudgetCalculations
                .Where(x => !x.IsDeleted && x.IsActive &&
                    (x.EventTypeId == eventTypeId || (typeName != null && x.Category != null && x.Category.ToLower() == typeName)))
                .OrderBy(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEventTypeIdAsync));
            throw;
        }
    }

    public async Task AddAsync(BudgetCalculation budgetCalculation, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureColumnsAsync(cancellationToken);
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
