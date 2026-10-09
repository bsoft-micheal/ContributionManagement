using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentTransactionRepository> _logger;

    public PaymentTransactionRepository(ApplicationDbContext context, ILogger<PaymentTransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PaymentTransactions
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventName) && !eventName.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanEvent = eventName.Trim().ToLower();
                query = query.Where(x => x.Event != null && x.Event.EventName.ToLower() == cleanEvent);
            }

            if (!string.IsNullOrWhiteSpace(mode) && !mode.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanMode = mode.Trim().ToLower();
                query = query.Where(x => x.PaymentModeItem != null && x.PaymentModeItem.PaymentModeName.ToLower() == cleanMode);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanStatus = status.Trim().ToLower();
                query = query.Where(x => x.StatusItem != null && x.StatusItem.StatusName.ToLower() == cleanStatus);
            }

            if (startDate.HasValue)
            {
                query = query.Where(x => x.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(x => x.PaymentDate <= endDate.Value);
            }

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await query
                .OrderByDescending(x => x.PaymentDate)
                .Select(x => new PaymentTransactionDto
                {
                    TransactionId = x.TransactionId,
                    TxnNumber = x.TxnNumber,
                    EventId = x.EventId,
                    UserId = x.UserId,
                    MemberName = x.User != null ? (!string.IsNullOrWhiteSpace(x.User.FullName) ? x.User.FullName : x.User.Username) : string.Empty,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    Amount = x.Amount,
                    PaymentDate = x.PaymentDate,
                    PaymentMode = x.PaymentModeItem != null ? x.PaymentModeItem.PaymentModeName : string.Empty,
                    Utr = x.Utr,
                    Status = x.StatusItem != null ? x.StatusItem.StatusName : (!string.IsNullOrWhiteSpace(x.VerifiedBy) ? CommonConstants.PaymentStatuses.Verified : CommonConstants.PaymentStatuses.Pending),
                    VerifiedBy = x.VerifiedBy,
                    VerifiedOn = x.VerifiedOn,
                    Notes = x.Notes,
                    Screenshot = x.Screenshot,
                    ParentTxnNumber = x.ParentTxnNumber,
                    TransactionGroupId = x.TransactionGroupId,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
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

    public async Task<PaymentTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PaymentTransactions
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.PaymentModeItem)
                .Include(x => x.StatusItem)
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.PaymentTransactions.AddAsync(transaction, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(PaymentTransaction transaction)
    {
        try
        {
            _context.PaymentTransactions.Update(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(PaymentTransaction transaction)
    {
        try
        {
            transaction.IsDeleted = true;
            transaction.ModifiedOn = DateTime.UtcNow;
            _context.PaymentTransactions.Update(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }

    public async Task<List<PaymentTransaction>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PaymentTransactions
                .Include(x => x.User)
                .Include(x => x.Event)
                .Include(x => x.PaymentModeItem)
                .Include(x => x.StatusItem)
                .Where(x => x.TransactionGroupId == groupId && !x.IsDeleted)
                .OrderBy(x => x.TxnNumber)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByGroupIdAsync));
            throw;
        }
    }

    public async Task<string> GetNextTxnNumberAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE sequencename = 'payment_txn_number_seq') THEN
                        CREATE SEQUENCE payment_txn_number_seq START WITH 1250 INCREMENT BY 1;
                        PERFORM setval('payment_txn_number_seq', 
                            GREATEST(
                                COALESCE((SELECT MAX(CAST(SUBSTRING(REGEXP_REPLACE(txn_number, '^TXN0*', '') FROM '^[0-9]+') AS INTEGER)) 
                                          FROM payment_transactions 
                                          WHERE txn_number LIKE 'TXN%'), 1250), 
                                1250
                            )
                        );
                    END IF;
                END $$;
                SELECT nextval('payment_txn_number_seq')::bigint;";

            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            var valObj = await cmd.ExecuteScalarAsync(cancellationToken);
            long nextVal = Convert.ToInt64(valObj);
            return $"{CommonConstants.Defaults.TxnPrefix}{nextVal:D6}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating next transaction number via sequence. Falling back to query.");
            var maxNum = await _context.PaymentTransactions
                .Where(x => x.TxnNumber.StartsWith("TXN"))
                .Select(x => x.TxnNumber)
                .ToListAsync(cancellationToken);

            long highest = 1250;
            foreach (var txn in maxNum)
            {
                var match = System.Text.RegularExpressions.Regex.Match(txn, @"TXN0*(\d+)");
                if (match.Success && long.TryParse(match.Groups[1].Value, out var n))
                {
                    if (n > highest) highest = n;
                }
            }
            return $"{CommonConstants.Defaults.TxnPrefix}{(highest + 1):D6}";
        }
    }
}
