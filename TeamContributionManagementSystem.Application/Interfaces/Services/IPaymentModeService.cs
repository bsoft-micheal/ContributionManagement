using TeamContributionManagementSystem.Application.DTOs.PaymentModes;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IPaymentModeService
{
    Task<IReadOnlyCollection<PaymentModeDto>> GetAllPaymentModeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<PaymentModeDto?> GetPaymentModeAsyncById(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentModeDto> SavePaymentModeAsync(CreatePaymentModeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<PaymentModeDto> UpdatePaymentModeAsyncById(Guid id, UpdatePaymentModeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeletePaymentModeAsyncById(Guid id, CancellationToken cancellationToken = default);
}
