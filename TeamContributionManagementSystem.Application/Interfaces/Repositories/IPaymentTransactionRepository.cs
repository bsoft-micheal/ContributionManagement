using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IPaymentTransactionRepository
{
    Task<List<PaymentTransaction>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    void Update(PaymentTransaction transaction);
    void Delete(PaymentTransaction transaction);

    // Standardized naming
    Task<List<PaymentTransaction>> GetAllPaymentAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
    Task<PaymentTransaction?> GetPaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => GetByIdAsync(transactionId, cancellationToken);
    Task SavePaymentAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default) => AddAsync(transaction, cancellationToken);
    void UpdatePaymentAsyncById(PaymentTransaction transaction) => Update(transaction);
    void DeletePaymentAsyncById(PaymentTransaction transaction) => Delete(transaction);
}
