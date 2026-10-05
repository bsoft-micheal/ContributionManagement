using TeamContributionManagementSystem.Application.DTOs.Priorities;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IPriorityService
{
    Task<IReadOnlyCollection<PriorityDto>> GetAllPriorityAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<PriorityDto?> GetPriorityAsyncById(Guid id, CancellationToken cancellationToken = default);
    Task<PriorityDto> SavePriorityAsync(CreatePriorityRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<PriorityDto> UpdatePriorityAsyncById(Guid id, UpdatePriorityRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeletePriorityAsyncById(Guid id, CancellationToken cancellationToken = default);
}
