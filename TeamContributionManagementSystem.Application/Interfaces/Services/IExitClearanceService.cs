using TeamContributionManagementSystem.Application.DTOs.ExitClearance;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IExitClearanceService
{
    Task<ExitClearanceQueueResponseDto> GetQueueAsync(
        string? search = null,
        Guid? eventId = null,
        string? clearanceStatus = null,
        string? paymentStatus = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ExitClearanceDuesDetailDto> GetDuesDetailAsync(Guid clearanceId, CancellationToken cancellationToken = default);

    Task<ExitClearanceSummaryDto> ApproveExitAsync(
        Guid clearanceId,
        ApproveExitRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExitClearanceHistoryDto>> GetHistoryAsync(
        string? search = null,
        Guid? eventId = null,
        Guid? approvedBy = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<ExitClearanceHistoryDto> GetHistoryDetailAsync(Guid historyId, CancellationToken cancellationToken = default);

    Task SyncSendOffBeneficiariesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task SyncAllSendOffEventsAsync(CancellationToken cancellationToken = default);
    Task HandleEventDeletedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
