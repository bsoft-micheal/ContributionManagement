using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
            await EnsureSchemaAsync(cancellationToken);
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

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await query
                .OrderByDescending(x => x.ExpenseDate)
                .ThenByDescending(x => x.ModifiedOn ?? x.CreatedAt)
                .Select(x => new
                {
                    Expense = new ExpenseDto
                    {
                        ExpenseId = x.ExpenseId,
                        ExpenseNo = x.ExpenseNo,
                        ExpenseNumber = x.ExpenseNo,
                        EventName = x.EventName,
                        Category = x.Category,
                        Amount = x.Amount,
                        BudgetAmount = x.BudgetAmount,
                        ExtraAmount = x.ExtraAmount,
                        ExpenseDate = x.ExpenseDate,
                        Status = x.Status,
                        SubmittedBy = x.SubmittedBy,
                        ApprovedBy = x.ApprovedBy,
                        Description = x.Description,
                        FileName = x.FileName,
                        FileUrl = x.FileName,
                        IsActive = x.IsActive,
                        CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                        CreatedAt = x.CreatedAt,
                        CreatedOn = x.CreatedAt,
                        ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                        ModifiedOn = x.ModifiedOn
                    },
                    AdditionalExpensesJson = x.AdditionalExpenses
                })
                .ToListAsync(cancellationToken);

            var result = new List<ExpenseDto>(items.Count);
            foreach (var wrapper in items)
            {
                var item = wrapper.Expense;
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }

                TeamContributionManagementSystem.Application.Common.ExpenseMetadataHelper.UnpackDescription(
                    item.Description,
                    item.Amount,
                    out var cleanDesc,
                    out var legacyBudgetAmt,
                    out var legacyExtraAmt,
                    out var legacyAddRows);

                item.Description = cleanDesc;

                if (item.BudgetAmount > 0 || item.ExtraAmount > 0 || !string.IsNullOrWhiteSpace(wrapper.AdditionalExpensesJson))
                {
                    if (!string.IsNullOrWhiteSpace(wrapper.AdditionalExpensesJson))
                    {
                        try
                        {
                            item.AdditionalExpenses = System.Text.Json.JsonSerializer.Deserialize<List<ExpenseAdditionalItemDto>>(
                                wrapper.AdditionalExpensesJson,
                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<ExpenseAdditionalItemDto>();
                        }
                        catch
                        {
                            item.AdditionalExpenses = legacyAddRows;
                        }
                    }
                    else
                    {
                        item.AdditionalExpenses = legacyAddRows;
                    }
                }
                else
                {
                    item.BudgetAmount = legacyBudgetAmt;
                    item.ExtraAmount = legacyExtraAmt;
                    item.AdditionalExpenses = legacyAddRows;
                }

                result.Add(item);
            }

            return result;
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
            await EnsureSchemaAsync(cancellationToken);
            return await _context.Expenses.FirstOrDefaultAsync(x => x.ExpenseId == expenseId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<Expense?> GetByIdWithDeletedAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Expenses.FirstOrDefaultAsync(x => x.ExpenseId == expenseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdWithDeletedAsync));
            throw;
        }
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
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

    private static bool _schemaEnsured = false;
    private static readonly SemaphoreSlim _schemaLock = new(1, 1);

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaEnsured) return;
        await _schemaLock.WaitAsync(cancellationToken);
        try
        {
            if (_schemaEnsured) return;
            if (_context.Database.IsNpgsql())
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var cmd = conn.CreateCommand();
                var currentTx = _context.Database.CurrentTransaction?.GetDbTransaction();
                if (currentTx != null)
                {
                    cmd.Transaction = currentTx;
                }

                cmd.CommandText = @"
                    DO $$
                    BEGIN
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'expenses' AND column_name = 'expense_number') THEN
                            ALTER TABLE expenses RENAME COLUMN expense_number TO expense_no;
                        END IF;
                    END $$;
                    ALTER TABLE IF EXISTS expenses ADD COLUMN IF NOT EXISTS expense_no VARCHAR(30);
                    CREATE TABLE IF NOT EXISTS expense_sequences (
                        id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                        year INT NOT NULL,
                        month INT NOT NULL,
                        last_sequence INT NOT NULL DEFAULT 0,
                        updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_expense_sequences_year_month ON expense_sequences (year, month);
                    CREATE UNIQUE INDEX IF NOT EXISTS ix_expenses_expense_no ON expenses (expense_no) WHERE is_deleted = false AND expense_no IS NOT NULL AND expense_no <> '';
                ";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            _schemaEnsured = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to automatically ensure expense schema: {Message}", ex.Message);
        }
        finally
        {
            _schemaLock.Release();
        }
    }

    private static readonly SemaphoreSlim _sequenceLock = new(1, 1);

    public async Task<string> GetNextExpenseNumberAsync(DateTime expenseDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureSchemaAsync(cancellationToken);
            int year = expenseDate.Year;
            int month = expenseDate.Month;

            // Check if any active expenses exist for this month in the database
            bool hasExistingForMonth = await _context.Expenses
                .AnyAsync(e => !e.IsDeleted && e.ExpenseDate.Year == year && e.ExpenseDate.Month == month, cancellationToken);

            // In PostgreSQL runtime, execute atomic upsert with row-lock to guarantee concurrency safety
            if (_context.Database.IsNpgsql())
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var cmd = conn.CreateCommand();
                var currentTx = _context.Database.CurrentTransaction?.GetDbTransaction();
                if (currentTx != null)
                {
                    cmd.Transaction = currentTx;
                }

                // Get max numeric suffix from all existing expenses for this year and month in the database (including soft-deleted)
                // Format: EXPyyyyMMdddd (e.g., EXP2026100003 -> prefix length is 9, suffix is 4 digits)
                var prefix = $"EXP{year:D4}{month:D2}";
                cmd.CommandText = @"
                    SELECT COALESCE(MAX(
                        CASE 
                            WHEN length(expense_no) = 13 AND substring(expense_no from 10) ~ '^[0-9]+$' 
                            THEN substring(expense_no from 10)::INTEGER 
                            ELSE 0 
                        END
                    ), 0)
                    FROM expenses 
                    WHERE expense_no LIKE @prefix || '%';
                ";
                var pPref = cmd.CreateParameter();
                pPref.ParameterName = "@prefix";
                pPref.Value = prefix;
                cmd.Parameters.Add(pPref);

                var maxExpenseNumScalar = await cmd.ExecuteScalarAsync(cancellationToken);
                int maxExistingExpenseNum = Convert.ToInt32(maxExpenseNumScalar);
                cmd.Parameters.Clear();

                if (!hasExistingForMonth && maxExistingExpenseNum == 0)
                {
                    // If all expenses were deleted/cleared completely from the database, reset sequence counter for this month
                    cmd.CommandText = "DELETE FROM expense_sequences WHERE year = @year AND month = @month;";
                    var pY = cmd.CreateParameter();
                    pY.ParameterName = "@year";
                    pY.Value = year;
                    cmd.Parameters.Add(pY);

                    var pM = cmd.CreateParameter();
                    pM.ParameterName = "@month";
                    pM.Value = month;
                    cmd.Parameters.Add(pM);

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    cmd.Parameters.Clear();
                }

                // Increment sequence ensuring it is always strictly greater than both last_sequence and maxExistingExpenseNum
                cmd.CommandText = @"
                    INSERT INTO expense_sequences (id, year, month, last_sequence, updated_at)
                    VALUES (gen_random_uuid(), @year, @month, GREATEST(@maxExisting, 0) + 1, timezone('utc', now()))
                    ON CONFLICT (year, month)
                    DO UPDATE SET last_sequence = GREATEST(expense_sequences.last_sequence, @maxExisting) + 1, updated_at = timezone('utc', now())
                    RETURNING last_sequence;
                ";

                var pYear = cmd.CreateParameter();
                pYear.ParameterName = "@year";
                pYear.Value = year;
                cmd.Parameters.Add(pYear);

                var pMonth = cmd.CreateParameter();
                pMonth.ParameterName = "@month";
                pMonth.Value = month;
                cmd.Parameters.Add(pMonth);

                var pMax = cmd.CreateParameter();
                pMax.ParameterName = "@maxExisting";
                pMax.Value = maxExistingExpenseNum;
                cmd.Parameters.Add(pMax);

                var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                int nextSequence = Convert.ToInt32(scalar);
                return $"EXP{year:D4}{month:D2}{nextSequence:D4}";
            }

            // Fallback for InMemory / SQLite / Test contexts:
            await _sequenceLock.WaitAsync(cancellationToken);
            try
            {
                var prefix = $"EXP{year:D4}{month:D2}";
                var allExpenseNos = await _context.Expenses
                    .Where(e => e.ExpenseNo != null && e.ExpenseNo.StartsWith(prefix))
                    .Select(e => e.ExpenseNo)
                    .ToListAsync(cancellationToken);

                int maxInMemoryExpenseNum = 0;
                foreach (var expNo in allExpenseNos)
                {
                    if (expNo != null && expNo.Length == 13 && int.TryParse(expNo.Substring(9), out var parsedNum))
                    {
                        if (parsedNum > maxInMemoryExpenseNum) maxInMemoryExpenseNum = parsedNum;
                    }
                }

                var seqRecord = await _context.ExpenseSequences
                    .FirstOrDefaultAsync(s => s.Year == year && s.Month == month, cancellationToken);

                if (!hasExistingForMonth && maxInMemoryExpenseNum == 0 && seqRecord != null)
                {
                    _context.ExpenseSequences.Remove(seqRecord);
                    await _context.SaveChangesAsync(cancellationToken);
                    seqRecord = null;
                }

                int nextSequence;
                if (seqRecord == null)
                {
                    nextSequence = Math.Max(maxInMemoryExpenseNum, 0) + 1;
                    seqRecord = new ExpenseSequence
                    {
                        Id = Guid.NewGuid(),
                        Year = year,
                        Month = month,
                        LastSequence = nextSequence,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _context.ExpenseSequences.AddAsync(seqRecord, cancellationToken);
                }
                else
                {
                    nextSequence = Math.Max(seqRecord.LastSequence, maxInMemoryExpenseNum) + 1;
                    seqRecord.LastSequence = nextSequence;
                    seqRecord.UpdatedAt = DateTime.UtcNow;
                    _context.ExpenseSequences.Update(seqRecord);
                }

                await _context.SaveChangesAsync(cancellationToken);
                return $"EXP{year:D4}{month:D2}{nextSequence:D4}";
            }
            finally
            {
                _sequenceLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetNextExpenseNumberAsync));
            throw;
        }
    }
}
