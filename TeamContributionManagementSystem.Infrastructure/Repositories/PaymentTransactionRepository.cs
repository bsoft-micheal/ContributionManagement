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
                    Status = x.StatusItem != null ? x.StatusItem.StatusName : string.Empty,
                    VerifiedBy = x.VerifiedBy,
                    VerifiedOn = x.VerifiedOn,
                    Notes = x.Notes,
                    Screenshot = x.Screenshot,
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
}
