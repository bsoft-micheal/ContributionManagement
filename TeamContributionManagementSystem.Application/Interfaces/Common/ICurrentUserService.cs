namespace TeamContributionManagementSystem.Application.Interfaces.Common;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? MemberId { get; }
    bool IsMemberRole { get; }
    bool IsAuthenticated { get; }
}
