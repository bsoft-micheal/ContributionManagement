using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthResponseDto GenerateToken(
        AppUser user, 
        Guid? sessionId = null, 
        Guid? memberId = null, 
        IEnumerable<string>? roles = null, 
        IEnumerable<Guid>? roleIds = null)
    {
        return GenerateToken(user, sessionId, memberId, roles, roleIds, null, null);
    }

    public AuthResponseDto GenerateToken(
        AppUser user, 
        Guid? sessionId, 
        Guid? memberId, 
        IEnumerable<string>? roles, 
        IEnumerable<Guid>? roleIds,
        string? activeRole,
        Guid? activeRoleId)
    {
        var secret = _configuration[CommonConstants.ConfigKeys.JwtSecret]
            ?? throw new InvalidOperationException(CommonMessages.Auth.JwtSecretNotConfigured);

        var issuer = _configuration[CommonConstants.ConfigKeys.JwtIssuer] ?? CommonConstants.Defaults.JwtIssuer;
        var audience = _configuration[CommonConstants.ConfigKeys.JwtAudience] ?? CommonConstants.Defaults.JwtAudience;
        var expiryMinutes = int.TryParse(_configuration[CommonConstants.ConfigKeys.JwtExpiryMinutes], out var configuredValue) ? configuredValue : 120;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var roleNamesFromEntity = user.UserRoles?.Select(ur => ur.Role?.RoleName).Where(r => !string.IsNullOrEmpty(r)).Select(r => r!) ?? new[] { user.Role.ToString() };
        var resolvedRoles = (roles ?? roleNamesFromEntity)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resolvedRoles.Count == 0)
        {
            resolvedRoles.Add(user.Role.ToString());
        }

        var resolvedRoleIds = (roleIds ?? user.UserRoles?.Select(ur => ur.RoleId) ?? Enumerable.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var activeUserRoles = user.UserRoles?.ToList() ?? new List<AppUserRole>();
        var primaryRolesList = activeUserRoles
            .Where(ur => ur.IsPrimary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName))
            .Select(ur => ur.Role!.RoleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var secondaryRolesList = activeUserRoles
            .Where(ur => ur.IsSecondary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName))
            .Select(ur => ur.Role!.RoleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (primaryRolesList.Count == 0 && resolvedRoles.Count > 0)
        {
            primaryRolesList.Add(resolvedRoles.First());
        }

        string effectiveActiveRole;
        if (!string.IsNullOrWhiteSpace(activeRole) && resolvedRoles.Contains(activeRole, StringComparer.OrdinalIgnoreCase))
        {
            effectiveActiveRole = activeRole;
        }
        else if (user.RoleId.HasValue && activeUserRoles.Any(ur => ur.RoleId == user.RoleId.Value && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)))
        {
            effectiveActiveRole = activeUserRoles.First(ur => ur.RoleId == user.RoleId.Value).Role!.RoleName;
        }
        else if (primaryRolesList.Count > 0)
        {
            effectiveActiveRole = primaryRolesList.First();
        }
        else
        {
            effectiveActiveRole = resolvedRoles.FirstOrDefault() ?? CommonRoles.Member;
        }

        Guid? effectiveActiveRoleId = activeRoleId;
        if (!effectiveActiveRoleId.HasValue || effectiveActiveRoleId.Value == Guid.Empty)
        {
            effectiveActiveRoleId = activeUserRoles.FirstOrDefault(ur => ur.Role != null && string.Equals(ur.Role.RoleName, effectiveActiveRole, StringComparison.OrdinalIgnoreCase))?.RoleId;
            if (!effectiveActiveRoleId.HasValue && resolvedRoleIds.Count > 0)
            {
                effectiveActiveRoleId = resolvedRoleIds.First();
            }
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("role", effectiveActiveRole),
            new("activeRole", effectiveActiveRole)
        };

        if (effectiveActiveRoleId.HasValue)
        {
            claims.Add(new Claim("activeRoleId", effectiveActiveRoleId.Value.ToString()));
        }

        foreach (var r in resolvedRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        foreach (var rId in resolvedRoleIds)
        {
            claims.Add(new Claim("role_id", rId.ToString()));
        }

        claims.Add(new Claim("user_id", user.UserId.ToString()));
        claims.Add(new Claim("userId", user.UserId.ToString()));

        var effectiveMemberId = memberId ?? user.UserId;
        claims.Add(new Claim("member_id", effectiveMemberId.ToString()));
        claims.Add(new Claim("memberId", effectiveMemberId.ToString()));

        if (sessionId.HasValue)
        {
            claims.Add(new Claim(CommonConstants.Defaults.SessionIdClaim, sessionId.Value.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var primaryRoleIdsList = activeUserRoles
            .Where(ur => ur.IsPrimary && ur.RoleId != Guid.Empty)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToList();

        var secondaryRoleIdsList = activeUserRoles
            .Where(ur => ur.IsSecondary && ur.RoleId != Guid.Empty)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToList();

        if (primaryRoleIdsList.Count == 0 && effectiveActiveRoleId.HasValue)
        {
            primaryRoleIdsList.Add(effectiveActiveRoleId.Value);
        }

        return new AuthResponseDto
        {
            UserId = user.UserId,
            MemberId = effectiveMemberId,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            FullName = user.FullName,
            Role = effectiveActiveRole,
            RoleId = effectiveActiveRoleId,
            PrimaryRoles = primaryRolesList,
            PrimaryRoleIds = primaryRoleIdsList,
            SecondaryRoles = secondaryRolesList,
            SecondaryRoleIds = secondaryRoleIdsList,
            EnableMultipleRoles = user.EnableMultipleRoles,
            IsPrimary = user.IsPrimary || activeUserRoles.Any(ur => ur.IsPrimary),
            IsSecondary = user.IsSecondary || activeUserRoles.Any(ur => ur.IsSecondary),
            ActiveRoleId = effectiveActiveRoleId,
            ProfileImage = user.ProfileImage,
            ExpiresAtUtc = expiresAtUtc,
            IsFirstLogin = user.IsFirstLogin
        };
    }
}
