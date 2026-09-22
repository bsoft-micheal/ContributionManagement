using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

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
    Task<bool> HasMembersAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Update(Role role);
    void Delete(Role role);
}

public interface IEventTypeRepository
{
    Task<List<EventType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventType?> GetByIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task<EventType?> GetByNameAsync(string eventTypeName, CancellationToken cancellationToken = default);
    Task<bool> HasEventsAsync(Guid eventTypeId, CancellationToken cancellationToken = default);
    Task AddAsync(EventType eventType, CancellationToken cancellationToken = default);
    void Update(EventType eventType);
    void Delete(EventType eventType);
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
    void DeleteParticipants(IEnumerable<EventParticipant> participants);
}

public interface IContributionRepository
{
    Task<List<Contribution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<List<Contribution>> GetByMemberEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default);
    void Update(Contribution contribution);
    void DeleteRange(IEnumerable<Contribution> contributions);
}

public interface IUserRepository
{
    Task<List<AppUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AppUser?> GetFirstAdminAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
    void Update(AppUser user);
    void Delete(AppUser user);
}

public interface IRoleRightRepository
{
    Task<List<RoleRight>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<RoleRight>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default);
}

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    void Update(Expense expense);
    void Delete(Expense expense);
}

public interface ISupportTicketRepository
{
    Task<List<SupportTicket>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByTicketNoAsync(string ticketNo, CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    void Update(SupportTicket ticket);
    void Delete(SupportTicket ticket);
}

public interface ISystemSettingRepository
{
    Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SystemSetting> settings, CancellationToken cancellationToken = default);
    void Update(SystemSetting setting);
}

public interface IPaymentTransactionRepository
{
    Task<List<PaymentTransaction>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    void Update(PaymentTransaction transaction);
    void Delete(PaymentTransaction transaction);
}

public interface IGalleryRepository
{
    Task<List<GalleryPhoto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default);
    Task<GalleryPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default);
    Task AddAsync(GalleryPhoto photo, CancellationToken cancellationToken = default);
    void Delete(GalleryPhoto photo);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

