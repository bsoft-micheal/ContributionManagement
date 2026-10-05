namespace TeamContributionManagementSystem.Application.Interfaces.Common;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string? Role { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<Guid> RoleIds { get; }
    bool HasRole(string roleName);
    bool HasRoleId(Guid roleId);
    Guid? MemberId { get; }
    bool IsMemberRole { get; }
    bool IsMemberOnlyRole { get; }
    bool IsAuthenticated { get; }
}
