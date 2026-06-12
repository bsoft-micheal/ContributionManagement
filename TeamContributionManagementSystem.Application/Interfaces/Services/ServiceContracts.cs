using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Dashboard;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Reports;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Users;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IMemberService
{
    Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MemberDto> CreateAsync(CreateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberDto> UpdateAsync(Guid memberId, UpdateMemberRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid memberId, CancellationToken cancellationToken = default);
}

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default);
}

public interface IEventTypeService
{
    Task<IReadOnlyCollection<EventTypeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EventTypeDto> CreateAsync(CreateEventTypeRequestDto request, CancellationToken cancellationToken = default);
    Task<EventTypeDto> UpdateAsync(Guid eventTypeId, UpdateEventTypeRequestDto request, CancellationToken cancellationToken = default);
}

public interface IEventService
{
    Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> CreateAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
    Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default);
}

public interface IContributionService
{
    Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default);
    Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ReportsSummaryDto> GetSummaryAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default);
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
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRoleRightsService
{
    Task<IReadOnlyCollection<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RoleRightDto>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default);
    Task SaveRoleRightsAsync(UpdateRoleRightsRequestDto request, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
