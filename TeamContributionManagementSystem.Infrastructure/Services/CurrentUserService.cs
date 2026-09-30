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
            var allRoles = Roles;
            if (allRoles.Contains(CommonRoles.Admin, StringComparer.OrdinalIgnoreCase)) return CommonRoles.Admin;
            if (allRoles.Contains("Organizer", StringComparer.OrdinalIgnoreCase)) return "Organizer";
            if (allRoles.Contains(CommonRoles.Member, StringComparer.OrdinalIgnoreCase)) return CommonRoles.Member;
            return allRoles.FirstOrDefault()
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
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

            // Fallback: resolve MemberId via Email lookup
            var email = Email;
            if (!string.IsNullOrWhiteSpace(email) && _httpContextAccessor.HttpContext?.RequestServices != null)
            {
                var memberRepo = _httpContextAccessor.HttpContext.RequestServices.GetService<IMemberRepository>();
                if (memberRepo != null)
                {
                    var m = memberRepo.GetByEmailAsync(email.Trim()).GetAwaiter().GetResult();
                    if (m != null)
                    {
                        return m.MemberId;
                    }
                }
            }
            return null;
        }
    }

    public bool IsMemberRole => HasRole(CommonRoles.Member);

    public bool IsMemberOnlyRole =>
        HasRole(CommonRoles.Member) &&
        !HasRole(CommonRoles.Admin) &&
        !HasRole("Organizer");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
