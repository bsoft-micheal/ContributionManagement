using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    AuthResponseDto GenerateToken(AppUser user, Guid? sessionId = null);
}
