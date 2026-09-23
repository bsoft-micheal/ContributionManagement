using TeamContributionManagementSystem.Application.DTOs.Payments;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IPaymentTransactionService
{
    Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> VerifyAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid transactionId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllPaymentAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        => GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
    Task<PaymentTransactionDto> GetPaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => GetByIdAsync(transactionId, cancellationToken);
    Task<PaymentTransactionDto> SavePaymentAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<PaymentTransactionDto> VerifyPaymentAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default) => VerifyAsync(transactionId, request, user, cancellationToken);
    Task DeletePaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => DeleteAsync(transactionId, cancellationToken);
}
