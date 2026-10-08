using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class PaymentModeRepository : IPaymentModeRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentModeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PaymentModeItem>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PaymentModes
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.PaymentModeName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentModeItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentModes
            .FirstOrDefaultAsync(x => x.PaymentModeId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<PaymentModeItem?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        return await _context.PaymentModes
            .FirstOrDefaultAsync(x => x.PaymentModeName.ToLower() == name.Trim().ToLower(), cancellationToken);
    }

    public async Task<bool> IsInUseAsync(string paymentModeName, CancellationToken cancellationToken = default)
    {
        var cleanName = paymentModeName.Trim().ToLower();

        var hasTxns = await _context.PaymentTransactions
            .AnyAsync(p => !p.IsDeleted && p.PaymentModeItem != null && p.PaymentModeItem.PaymentModeName.ToLower() == cleanName, cancellationToken);
        if (hasTxns) return true;

        var hasContributions = await _context.Contributions
            .AnyAsync(c => !c.IsDeleted && c.PaymentModeItem != null && c.PaymentModeItem.PaymentModeName.ToLower() == cleanName, cancellationToken);
        if (hasContributions) return true;

        return false;
    }

    public async Task AddAsync(PaymentModeItem paymentMode, CancellationToken cancellationToken = default)
    {
        await _context.PaymentModes.AddAsync(paymentMode, cancellationToken);
    }

    public void Update(PaymentModeItem paymentMode)
    {
        _context.PaymentModes.Update(paymentMode);
    }

    public void Delete(PaymentModeItem paymentMode)
    {
        paymentMode.IsDeleted = true;
        _context.PaymentModes.Update(paymentMode);
    }
}
