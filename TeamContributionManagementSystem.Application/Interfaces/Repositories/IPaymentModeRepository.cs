using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IPaymentModeRepository
{
    Task<IReadOnlyCollection<PaymentModeItem>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<PaymentModeItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentModeItem?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentModeItem paymentMode, CancellationToken cancellationToken = default);
    void Update(PaymentModeItem paymentMode);
    void Delete(PaymentModeItem paymentMode);
}
