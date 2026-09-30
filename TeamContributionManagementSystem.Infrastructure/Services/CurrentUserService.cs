using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;

namespace TeamContributionManagementSystem.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("userId")?.Value
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? user.Identity?.Name;
        }
    }

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public string? Role =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

    public Guid? MemberId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var val = user.FindFirst("member_id")?.Value
                ?? user.FindFirst("memberId")?.Value
                ?? user.FindFirst(ClaimTypes.PrimarySid)?.Value;

            if (Guid.TryParse(val, out var memberGuid))
            {
                return memberGuid;
            }

            if (Guid.TryParse(UserId, out var userGuid))
            {
                return userGuid;
            }

            // Fallback: resolve via Email lookup
            var email = Email;
            if (!string.IsNullOrWhiteSpace(email) && _httpContextAccessor.HttpContext?.RequestServices != null)
            {
                var userRepo = _httpContextAccessor.HttpContext.RequestServices.GetService<IUserRepository>();
                if (userRepo != null)
                {
                    var u = userRepo.GetByEmailAsync(email.Trim()).GetAwaiter().GetResult();
                    if (u != null)
                    {
                        return u.UserId;
                    }
                }
            }
            return null;
        }
    }

    public bool IsMemberRole =>
        string.Equals(Role, CommonRoles.Member, StringComparison.OrdinalIgnoreCase);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
