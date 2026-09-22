using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<PaymentTransactionRepository> _logger;

    public PaymentTransactionRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<PaymentTransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PaymentTransaction>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.PaymentTransactions.Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventName) && eventName != "ALL")
        {
            query = query.Where(x => x.EventName.ToLower() == eventName.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(mode) && mode != "ALL")
        {
            query = query.Where(x => x.PaymentMode.ToLower() == mode.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            query = query.Where(x => x.Status.ToLower() == status.ToLower());
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.PaymentDate <= endDate.Value);
        }

        return await query.OrderByDescending(x => x.PaymentDate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<PaymentTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PaymentTransactions.FirstOrDefaultAsync(x => x.TransactionId == transactionId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
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
            _logger.LogError(ex, "Error in AddAsync");
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
            _logger.LogError(ex, "Error in Update");
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
            _logger.LogError(ex, "Error in Delete");
            throw;
        }
    }
}
