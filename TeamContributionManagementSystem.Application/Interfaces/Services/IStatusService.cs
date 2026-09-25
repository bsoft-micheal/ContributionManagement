using TeamContributionManagementSystem.Application.DTOs.Statuses;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IStatusService
{
    Task<IReadOnlyCollection<StatusDto>> GetAllStatusAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<StatusDto?> GetStatusAsyncById(Guid id, CancellationToken cancellationToken = default);
    Task<StatusDto> SaveStatusAsync(CreateStatusRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<StatusDto> UpdateStatusAsyncById(Guid id, UpdateStatusRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteStatusAsyncById(Guid id, CancellationToken cancellationToken = default);
}
