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

        string primaryRole = resolvedRoles.Contains(CommonRoles.Admin, StringComparer.OrdinalIgnoreCase)
            ? CommonRoles.Admin
            : (resolvedRoles.Contains("Organizer", StringComparer.OrdinalIgnoreCase)
                ? "Organizer"
                : (resolvedRoles.Contains(CommonRoles.Member, StringComparer.OrdinalIgnoreCase)
                    ? CommonRoles.Member
                    : (resolvedRoles.FirstOrDefault() ?? CommonRoles.Member)));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("role", primaryRole)
        };

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

        return new AuthResponseDto
        {
            UserId = user.UserId,
            MemberId = effectiveMemberId,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            FullName = user.FullName,
            Role = primaryRole,
            Roles = resolvedRoles,
            RoleIds = resolvedRoleIds,
            ProfileImage = user.ProfileImage,
            ExpiresAtUtc = expiresAtUtc,
            IsFirstLogin = user.IsFirstLogin
        };
    }
}
