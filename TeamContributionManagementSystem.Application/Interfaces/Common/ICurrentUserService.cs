namespace TeamContributionManagementSystem.Application.Interfaces.Common;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
