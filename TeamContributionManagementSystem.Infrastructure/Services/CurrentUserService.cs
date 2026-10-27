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

    public string? Role
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            // 1. Check explicit active role header from request
            var headerRole = httpContext?.Request.Headers["X-Active-Role"].FirstOrDefault()
                ?? httpContext?.Request.Headers["X-Role"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerRole))
            {
                return headerRole.Trim();
            }

            var user = httpContext?.User;
            // 2. Check activeRole claim from JWT
            var activeRole = user?.FindFirst("activeRole")?.Value;
            if (!string.IsNullOrWhiteSpace(activeRole)) return activeRole.Trim();

            // 3. Check role claim
            var roleClaim = user?.FindFirst("role")?.Value;
            if (!string.IsNullOrWhiteSpace(roleClaim)) return roleClaim.Trim();

            // 4. Default to ClaimTypes.Role or first role
            return user?.FindFirstValue(ClaimTypes.Role) ?? Roles.FirstOrDefault();
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return Array.Empty<string>();
            }

            return user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("role"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<Guid> RoleIds
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return Array.Empty<Guid>();
            }

            return user.FindAll("role_id")
                .Concat(user.FindAll("roleId"))
                .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }
    }

    public bool HasRole(string roleName) =>
        Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    public bool HasRoleId(Guid roleId) =>
        RoleIds.Contains(roleId);

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
                ?? user.FindFirst("user_id")?.Value
                ?? user.FindFirst("userId")?.Value
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
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

    public bool IsAdminRole
    {
        get
        {
            var currentRole = Role;
            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return string.Equals(currentRole, CommonRoles.Admin, StringComparison.OrdinalIgnoreCase);
            }
            return HasRole(CommonRoles.Admin);
        }
    }

    public bool IsOrganizerRole
    {
        get
        {
            var currentRole = Role;
            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return string.Equals(currentRole, "Organizer", StringComparison.OrdinalIgnoreCase);
            }
            return !HasRole(CommonRoles.Admin) && HasRole("Organizer");
        }
    }

    public bool IsMemberRole
    {
        get
        {
            var currentRole = Role;
            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return string.Equals(currentRole, CommonRoles.Member, StringComparison.OrdinalIgnoreCase);
            }
            return !HasRole(CommonRoles.Admin) && !HasRole("Organizer") && HasRole(CommonRoles.Member);
        }
    }

    public bool IsMemberOnlyRole =>
        HasRole(CommonRoles.Member) &&
        !HasRole(CommonRoles.Admin) &&
        !HasRole("Organizer");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
