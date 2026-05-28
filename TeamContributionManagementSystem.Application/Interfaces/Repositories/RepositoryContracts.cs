using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IMemberRepository
{
    Task<List<Member>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Member>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Member>> GetByIdsAsync(IEnumerable<Guid> memberIds, CancellationToken cancellationToken = default);
    Task<Member?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<Member>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default);
    Task AddAsync(Member member, CancellationToken cancellationToken = default);
    void Update(Member member);
}

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Update(Role role);
}

public interface IEventTypeRepository
{
    Task<List<EventType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventType?> GetByIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task<EventType?> GetByNameAsync(string eventTypeName, CancellationToken cancellationToken = default);
    Task AddAsync(EventType eventType, CancellationToken cancellationToken = default);
    void Update(EventType eventType);
}

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<List<Event>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> BirthdayEventExistsAsync(Guid memberId, int month, int year, CancellationToken cancellationToken = default);
    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);
    void Update(Event eventItem);
}

public interface IContributionRepository
{
    Task<List<Contribution>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default);
    void Update(Contribution contribution);
}

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetFirstAdminAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
