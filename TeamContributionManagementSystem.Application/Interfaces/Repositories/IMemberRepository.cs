using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IMemberRepository
{
    Task<List<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<MemberDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Member>> GetByIdsAsync(IEnumerable<Guid> memberIds, CancellationToken cancellationToken = default);
    Task<Member?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<MemberDto>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default);
    Task AddAsync(Member member, CancellationToken cancellationToken = default);
    void Update(Member member);

    // Standardized naming
    Task<List<MemberDto>> GetAllMemberAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<Member?> GetMemberAsyncById(Guid memberId, CancellationToken cancellationToken = default) => GetByIdAsync(memberId, cancellationToken);
    Task SaveMemberAsync(Member member, CancellationToken cancellationToken = default) => AddAsync(member, cancellationToken);
    void UpdateMemberAsyncById(Member member) => Update(member);
}
