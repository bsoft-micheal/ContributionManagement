using TeamContributionManagementSystem.Application.DTOs.Members;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IMemberService
{
    Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MemberDto> CreateAsync(CreateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<MemberDto> UpdateAsync(Guid memberId, UpdateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid memberId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<MemberDto>> GetAllMemberAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<MemberDto> SaveMemberAsync(CreateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<MemberDto> UpdateMemberAsyncById(Guid memberId, UpdateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(memberId, request, user, cancellationToken);
    Task DeleteMemberAsyncById(Guid memberId, CancellationToken cancellationToken = default) => DeleteAsync(memberId, cancellationToken);
}
