using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    AuthResponseDto GenerateToken(
        AppUser user, 
        Guid? sessionId = null, 
        Guid? memberId = null, 
        IEnumerable<string>? roles = null, 
        IEnumerable<Guid>? roleIds = null);

    AuthResponseDto GenerateToken(
        AppUser user, 
        Guid? sessionId, 
        Guid? memberId, 
        IEnumerable<string>? roles, 
        IEnumerable<Guid>? roleIds,
        string? activeRole,
        Guid? activeRoleId);
}
