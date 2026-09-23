using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IMemberService
{
    Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MemberDto> CreateAsync(CreateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberDto> UpdateAsync(Guid memberId, UpdateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid memberId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<MemberDto>> GetAllMemberAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<MemberDto> SaveMemberAsync(CreateMemberRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<MemberDto> UpdateMemberAsyncById(Guid memberId, UpdateMemberRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(memberId, request, cancellationToken);
    Task DeleteMemberAsyncById(Guid memberId, CancellationToken cancellationToken = default) => DeleteAsync(memberId, cancellationToken);
}

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<RoleDto>> GetAllRoleAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<RoleDto> SaveRoleAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<RoleDto> UpdateRoleAsyncById(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(roleId, request, cancellationToken);
    Task DeleteRoleAsyncById(Guid roleId, CancellationToken cancellationToken = default) => DeleteAsync(roleId, cancellationToken);
}

public interface IEventTypeService
{
    Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default);
    Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid eventTypeId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<EventTypeDto>> GetAllEventTypeAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<EventTypeDto> SaveEventTypeAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<EventTypeDto> UpdateEventTypeAsyncById(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(eventTypeId, request, cancellationToken);
    Task DeleteEventTypeAsyncById(Guid eventTypeId, CancellationToken cancellationToken = default) => DeleteAsync(eventTypeId, cancellationToken);
}

public interface IEventService
{
    Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> CreateAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<EventSummaryDto>> GetAllEventAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetAllAsync(month, year, cancellationToken);
    Task<EventDetailsDto> GetEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => GetByIdAsync(eventId, cancellationToken);
    Task<EventDetailsDto> SaveEventAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(createdByUserId, request, cancellationToken);
    Task<EventDetailsDto> UpdateEventAsyncById(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(eventId, request, cancellationToken);
    Task DeleteEventAsyncById(Guid eventId, CancellationToken cancellationToken = default) => DeleteAsync(eventId, cancellationToken);
}

public interface IContributionService
{
    Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<ContributionDto>> GetAllContributionAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<IReadOnlyCollection<ContributionDto>> GetContributionAsyncByEventId(Guid eventId, CancellationToken cancellationToken = default) => GetByEventIdAsync(eventId, cancellationToken);
    Task<ContributionDto> SavePayContributionAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default) => PayAsync(request, cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<DashboardSummaryDto> GetSummaryDashboardAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);
}

public interface IReportService
{
    Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<ReportsSummaryDto> GetSummaryReportAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default) => GetSummaryAsync(month, year, cancellationToken);
}

public interface IBirthdayAutomationService
{
    Task<int> CreateMonthlyBirthdayEventsAsync(CancellationToken cancellationToken = default);
}

public interface IUserManagementService
{
    Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<UserDto>> GetAllUserAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<UserDto> SaveUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);
    Task<UserDto> UpdateUserAsyncById(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default) => UpdateAsync(userId, request, cancellationToken);
    Task DeleteUserAsyncById(Guid userId, CancellationToken cancellationToken = default) => DeleteAsync(userId, cancellationToken);
}

public interface IRoleRightsService
{
    Task<IReadOnlyCollection<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleRightDto>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UpdateRoleRightsRequestDto request, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<RoleRightDto>> GetAllRoleRightAsync(CancellationToken cancellationToken = default) => GetAllAsync(cancellationToken);
    Task<IReadOnlyCollection<RoleRightDto>> GetRoleRightAsyncByRole(string roleName, CancellationToken cancellationToken = default) => GetByRoleAsync(roleName, cancellationToken);
}

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        IEnumerable<InlineEmailImage>? inlineImages = null,
        CancellationToken cancellationToken = default);
}

public sealed record InlineEmailImage(string ContentId, string FilePath, string? MediaType = null);

public interface IExpenseService
{
    Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<ExpenseDto> UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<ExpenseDto>> GetAllExpenseAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        => GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
    Task<ExpenseDto> GetExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => GetByIdAsync(expenseId, cancellationToken);
    Task<ExpenseDto> SaveExpenseAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<ExpenseDto> UpdateExpenseAsyncById(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(expenseId, request, user, cancellationToken);
    Task DeleteExpenseAsyncById(Guid expenseId, CancellationToken cancellationToken = default) => DeleteAsync(expenseId, cancellationToken);
}

public interface ISupportTicketService
{
    Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<SupportTicketDto>> GetAllSupportTicketAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
        => GetAllAsync(status, ticketType, priority, cancellationToken);
    Task<SupportTicketDto> GetSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => GetByIdAsync(ticketId, cancellationToken);
    Task<SupportTicketDto> SaveSupportTicketAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<SupportTicketDto> UpdateSupportTicketAsyncById(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => UpdateAsync(ticketId, request, user, cancellationToken);
    Task<SupportTicketDto> ReplySupportTicketAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default) => ReplyAsync(ticketId, request, user, cancellationToken);
    Task DeleteSupportTicketAsyncById(Guid ticketId, CancellationToken cancellationToken = default) => DeleteAsync(ticketId, cancellationToken);
}

public interface ISystemSettingService
{
    Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> UpdateSettingsAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default);
    Task<SystemSettingsDto> ResetSettingsAsync(CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<SystemSettingsDto> GetSettingAsync(CancellationToken cancellationToken = default) => GetSettingsAsync(cancellationToken);
    Task<SystemSettingsDto> UpdateSettingAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default) => UpdateSettingsAsync(settings, user, cancellationToken);
    Task<SystemSettingsDto> ResetSettingAsync(CancellationToken cancellationToken = default) => ResetSettingsAsync(cancellationToken);
}

public interface IPaymentTransactionService
{
    Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> VerifyAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid transactionId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllPaymentAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        => GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
    Task<PaymentTransactionDto> GetPaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => GetByIdAsync(transactionId, cancellationToken);
    Task<PaymentTransactionDto> SavePaymentAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task<PaymentTransactionDto> VerifyPaymentAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default) => VerifyAsync(transactionId, request, user, cancellationToken);
    Task DeletePaymentAsyncById(Guid transactionId, CancellationToken cancellationToken = default) => DeleteAsync(transactionId, cancellationToken);
}

public interface IGalleryService
{
    Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default);
    Task<GalleryPhotoDto> CreateAsync(CreateGalleryPhotoRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid photoId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllGalleryAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, cancellationToken);
    Task<GalleryPhotoDto> SaveGalleryAsync(CreateGalleryPhotoRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task DeleteGalleryAsyncById(Guid photoId, CancellationToken cancellationToken = default) => DeleteAsync(photoId, cancellationToken);
}
