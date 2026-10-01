using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.IdentityModel.Tokens;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.API.Filters;

/// <summary>
/// Hangfire dashboard authorization filter ensuring only authenticated Admin users can access the dashboard.
/// Supports ASP.NET Core ClaimsPrincipal, Authorization headers, Query String tokens (?token=...), and Cookies.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // 0. In local development or loopback, allow access for testing
        var env = httpContext.RequestServices.GetService<IWebHostEnvironment>();
        if (env != null && env.IsDevelopment())
        {
            return true;
        }

        // 1. Check if the current context user is already authenticated with Admin role
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            if (httpContext.User.IsInRole(CommonRoles.Admin) ||
                httpContext.User.HasClaim(ClaimTypes.Role, CommonRoles.Admin) ||
                httpContext.User.HasClaim("role", CommonRoles.Admin) ||
                httpContext.User.HasClaim(ClaimTypes.Role, "SuperAdmin") ||
                httpContext.User.HasClaim("role", "SuperAdmin"))
            {
                return true;
            }
        }

        // 2. Extract JWT token from Query string, Authorization header, or Cookie
        string? token = null;

        if (httpContext.Request.Query.TryGetValue("token", out var queryToken) && !string.IsNullOrWhiteSpace(queryToken))
        {
            token = queryToken.ToString();
        }
        else if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            var headerVal = authHeader.ToString();
            if (headerVal.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = headerVal.Substring(7).Trim();
            }
        }
        else if (httpContext.Request.Cookies.TryGetValue("token", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
        {
            token = cookieToken;
        }
        else if (httpContext.Request.Cookies.TryGetValue("jwt_token", out var jwtCookie) && !string.IsNullOrWhiteSpace(jwtCookie))
        {
            token = jwtCookie;
        }
        else if (httpContext.Request.Cookies.TryGetValue("access_token", out var accessCookie) && !string.IsNullOrWhiteSpace(accessCookie))
        {
            token = accessCookie;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var jwtSecret = configuration[CommonConstants.ConfigKeys.JwtSecret];
            if (string.IsNullOrWhiteSpace(jwtSecret))
            {
                return false;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (principal == null) return false;

            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value 
                ?? principal.FindFirst("role")?.Value;

            if (string.Equals(roleClaim, CommonRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(roleClaim, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
