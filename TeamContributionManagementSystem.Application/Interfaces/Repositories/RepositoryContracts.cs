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

    // Standardized naming
    Task<List<Member>> GetAllMemberAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<Member?> GetMemberAsyncById(Guid memberId, CancellationToken cancellationToken = default) => GetByIdAsync(memberId, cancellationToken);
    Task SaveMemberAsync(Member member, CancellationToken cancellationToken = default) => AddAsync(member, cancellationToken);
    void UpdateMemberAsyncById(Member member) => Update(member);
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

    // Standardized naming
    Task<List<Role>> GetAllRoleAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<Role?> GetRoleAsyncById(Guid roleId, CancellationToken cancellationToken = default) => GetByIdAsync(roleId, cancellationToken);
    Task SaveRoleAsync(Role role, CancellationToken cancellationToken = default) => AddAsync(role, cancellationToken);
    void UpdateRoleAsyncById(Role role) => Update(role);
    void DeleteRoleAsyncById(Role role) => Delete(role);
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

    // Standardized naming
    Task<List<EventType>> GetAllEventTypeAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<EventType?> GetEventTypeAsyncById(Guid eventTypeId, CancellationToken cancellationToken = default) => GetByIdAsync(eventTypeId, cancellationToken);
    Task SaveEventTypeAsync(EventType eventType, CancellationToken cancellationToken = default) => AddAsync(eventType, cancellationToken);
    void UpdateEventTypeAsyncById(EventType eventType) => Update(eventType);
    void DeleteEventTypeAsyncById(EventType eventType) => Delete(eventType);
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

    // Standardized naming
    Task<List<Event>> GetAllEventAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetAllAsync(month, year, cancellationToken);
    Task<Event?> GetEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => GetByIdAsync(eventId, cancellationToken);
    Task SaveEventAsync(Event eventItem, CancellationToken cancellationToken = default) => AddAsync(eventItem, cancellationToken);
    void UpdateEventAsyncById(Event eventItem) => Update(eventItem);
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

    // Standardized naming
    Task<List<Contribution>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<Contribution>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);
    void UpdateContributionAsyncById(Contribution contribution) => Update(contribution);
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

    // Standardized naming
    Task<List<AppUser>> GetAllUserAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<AppUser?> GetUserAsyncById(Guid userId, CancellationToken cancellationToken = default) => GetByIdAsync(userId, cancellationToken);
    Task SaveUserAsync(AppUser user, CancellationToken cancellationToken = default) => AddAsync(user, cancellationToken);
    void UpdateUserAsyncById(AppUser user) => Update(user);
    void DeleteUserAsyncById(AppUser user) => Delete(user);
}

public interface IRoleRightRepository
{
    Task<List<RoleRight>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<RoleRight>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UserRole role, IEnumerable<RoleRight> rights, CancellationToken cancellationToken = default);

    // Standardized naming
    Task<List<RoleRight>> GetAllRoleRightAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<List<RoleRight>> GetRoleRightAsyncByRole(UserRole role, CancellationToken cancellationToken = default) => GetByRoleAsync(role, cancellationToken);
}

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    void Update(Expense expense);
    void Delete(Expense expense);

    // Standardized naming
    Task<List<Expense>> GetAllExpenseAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
    Task<Expense?> GetExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => GetByIdAsync(expenseId, cancellationToken);
    Task SaveExpenseAsync(Expense expense, CancellationToken cancellationToken = default) => AddAsync(expense, cancellationToken);
    void UpdateExpenseAsyncById(Expense expense) => Update(expense);
    void DeleteExpenseAsyncById(Expense expense) => Delete(expense);
}

public interface ISupportTicketRepository
{
    Task<List<SupportTicket>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByTicketNoAsync(string ticketNo, CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    void Update(SupportTicket ticket);
    void Delete(SupportTicket ticket);

    // Standardized naming
    Task<List<SupportTicket>> GetAllSupportTicketAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default) => GetAllAsync(status, ticketType, priority, cancellationToken);
    Task<SupportTicket?> GetSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => GetByIdAsync(ticketId, cancellationToken);
    Task SaveSupportTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default) => AddAsync(ticket, cancellationToken);
    void UpdateSupportTicketAsyncById(SupportTicket ticket) => Update(ticket);
    void DeleteSupportTicketAsyncById(SupportTicket ticket) => Delete(ticket);
}

public interface ISystemSettingRepository
{
    Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SystemSetting> settings, CancellationToken cancellationToken = default);
    void Update(SystemSetting setting);

    // Standardized naming
    Task<List<SystemSetting>> GetAllSettingAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<SystemSetting?> GetSettingAsyncByKey(string key, CancellationToken cancellationToken = default) => GetByKeyAsync(key, cancellationToken);
    void UpdateSettingAsync(SystemSetting setting) => Update(setting);
}

public interface IPaymentTransactionRepository
{
    Task<List<PaymentTransaction>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    void Update(PaymentTransaction transaction);
    void Delete(PaymentTransaction transaction);

    // Standardized naming
    Task<List<PaymentTransaction>> GetAllPaymentAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
    Task<PaymentTransaction?> GetPaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => GetByIdAsync(transactionId, cancellationToken);
    Task SavePaymentAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default) => AddAsync(transaction, cancellationToken);
    void UpdatePaymentAsyncById(PaymentTransaction transaction) => Update(transaction);
    void DeletePaymentAsyncById(PaymentTransaction transaction) => Delete(transaction);
}

public interface IGalleryRepository
{
    Task<List<GalleryPhoto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default);
    Task<GalleryPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default);
    Task AddAsync(GalleryPhoto photo, CancellationToken cancellationToken = default);
    void Delete(GalleryPhoto photo);

    // Standardized naming
    Task<List<GalleryPhoto>> GetAllGalleryAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, cancellationToken);
    Task<GalleryPhoto?> GetGalleryAsyncById(Guid photoId, CancellationToken cancellationToken = default) => GetByIdAsync(photoId, cancellationToken);
    Task SaveGalleryAsync(GalleryPhoto photo, CancellationToken cancellationToken = default) => AddAsync(photo, cancellationToken);
    void DeleteGalleryAsyncById(GalleryPhoto photo) => Delete(photo);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
