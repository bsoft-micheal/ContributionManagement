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

    public AuthResponseDto GenerateToken(AppUser user, Guid? sessionId = null)
    {
        var secret = _configuration[CommonConstants.ConfigKeys.JwtSecret]
            ?? throw new InvalidOperationException(CommonMessages.Auth.JwtSecretNotConfigured);

        var issuer = _configuration[CommonConstants.ConfigKeys.JwtIssuer] ?? CommonConstants.Defaults.JwtIssuer;
        var audience = _configuration[CommonConstants.ConfigKeys.JwtAudience] ?? CommonConstants.Defaults.JwtAudience;
        var expiryMinutes = int.TryParse(_configuration[CommonConstants.ConfigKeys.JwtExpiryMinutes], out var configuredValue) ? configuredValue : 120;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

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
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            ProfileImage = user.ProfileImage,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
