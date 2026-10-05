using TeamContributionManagementSystem.Application.DTOs.WorkTypes;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IWorkTypeService
{
    Task<IReadOnlyCollection<WorkTypeDto>> GetAllWorkTypeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<WorkTypeDto?> GetWorkTypeAsyncById(Guid id, CancellationToken cancellationToken = default);
    Task<WorkTypeDto> SaveWorkTypeAsync(CreateWorkTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<WorkTypeDto> UpdateWorkTypeAsyncById(Guid id, UpdateWorkTypeRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteWorkTypeAsyncById(Guid id, CancellationToken cancellationToken = default);
}
