using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.API.RateLimiting;

/// <summary>
/// Extension methods for configuring and applying production-ready ASP.NET Core Rate Limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Registers ASP.NET Core native Rate Limiting services and policies with configurable parameters.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitSettings>() ?? new RateLimitSettings();
        services.AddSingleton(settings);

        if (!settings.EnableRateLimiting)
        {
            return services;
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = CommonStatusCodes.Status429TooManyRequests;

            // Global rejection handler returning unified ApiResponse structure and Retry-After header
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = CommonStatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = CommonConstants.ContentTypes.ApplicationJson;

                int retryAfterSeconds = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterSpan))
                {
                    retryAfterSeconds = Math.Max(1, (int)retryAfterSpan.TotalSeconds);
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                var responsePayload = new
                {
                    success = false,
                    statusCode = CommonStatusCodes.Status429TooManyRequests,
                    message = string.Format(CommonMessages.RateLimiting.TooManyRequestsFormat, retryAfterSeconds),
                    data = (object?)null
                };

                var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
                    .CreateLogger("RateLimiting");
                logger?.LogWarning(
                    "Rate limit exceeded on {Method} {Path} from {ClientKey}. Window resets in {RetryAfter}s.",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    GetPartitionKey(context.HttpContext, "rejected"),
                    retryAfterSeconds);

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(responsePayload),
                    cancellationToken);
            };

            // 1. Auth Policy: 5 req/min (Strict IP partition to prevent credential brute forcing)
            options.AddPolicy(CommonConstants.RateLimitPolicies.Auth, httpContext =>
            {
                var partitionKey = GetClientIpAddress(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"auth_{partitionKey}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Auth.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Auth.QueueLimit
                    });
            });

            // 2. General Policy: 100 req/min (Partitioned by User ID when authenticated, IP fallback)
            options.AddPolicy(CommonConstants.RateLimitPolicies.General, httpContext =>
            {
                var partitionKey = GetUserOrIpPartitionKey(httpContext, "gen");
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.General.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.General.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.General.QueueLimit
                    });
            });

            // 3. CRUD Policy: 60 req/min (Partitioned by User ID when authenticated, IP fallback)
            options.AddPolicy(CommonConstants.RateLimitPolicies.Crud, httpContext =>
            {
                var partitionKey = GetUserOrIpPartitionKey(httpContext, "crud");
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Crud.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Crud.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Crud.QueueLimit
                    });
            });

            // 4. Reports Policy: 10 req/min (Partitioned by User ID when authenticated, IP fallback)
            options.AddPolicy(CommonConstants.RateLimitPolicies.Reports, httpContext =>
            {
                var partitionKey = GetUserOrIpPartitionKey(httpContext, "rep");
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Reports.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Reports.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Reports.QueueLimit
                    });
            });

            // 5. Import/Export Policy: 15 req/min (Partitioned by User ID when authenticated, IP fallback)
            options.AddPolicy(CommonConstants.RateLimitPolicies.ImportExport, httpContext =>
            {
                var partitionKey = GetUserOrIpPartitionKey(httpContext, "ie");
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.ImportExport.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.ImportExport.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.ImportExport.QueueLimit
                    });
            });

            // 6. Payments Policy: 30 req/min (Partitioned by User ID when authenticated, IP fallback)
            options.AddPolicy(CommonConstants.RateLimitPolicies.Payments, httpContext =>
            {
                var partitionKey = GetUserOrIpPartitionKey(httpContext, "pay");
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Payments.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Payments.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Payments.QueueLimit
                    });
            });

            // 7. Global Limiter: Automatically protects all unannotated endpoints with appropriate categorizer
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                // Exempt internal background endpoints, health checks, Swagger, static files, and CORS preflight
                if (IsExemptRequest(httpContext))
                {
                    return RateLimitPartition.GetNoLimiter("exempt");
                }

                // If an endpoint has an explicit [EnableRateLimiting] attribute, the named policy handles it
                var endpoint = httpContext.GetEndpoint();
                if (endpoint != null && endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>() != null)
                {
                    return RateLimitPartition.GetNoLimiter("endpoint_policy_handled");
                }

                // Route categorizer based on URL path and HTTP verb
                var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                var method = httpContext.Request.Method;

                // Strict auth limits only for credential and OTP verification/requests (not switch-role or lookup)
                if (path.Contains("/auth/login") || path.Contains("/auth/verify-2fa") || path.Contains("/auth/forgot-password") || path.Contains("/mfa/verify"))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        $"auth_{GetClientIpAddress(httpContext)}",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.Auth.PermitLimit,
                            Window = TimeSpan.FromSeconds(settings.Auth.WindowSeconds),
                            QueueLimit = settings.Auth.QueueLimit
                        });
                }

                // Payment submission and proof upload operations
                if (path.Contains("/payments/submit-proof") || path.Contains("/payments/create") || path.Contains("/payments/save"))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        GetUserOrIpPartitionKey(httpContext, "pay"),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.Payments.PermitLimit,
                            Window = TimeSpan.FromSeconds(settings.Payments.WindowSeconds),
                            QueueLimit = settings.Payments.QueueLimit
                        });
                }

                if (path.Contains("savebulk") || path.Contains("/import") || path.Contains("/export"))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        GetUserOrIpPartitionKey(httpContext, "ie"),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.ImportExport.PermitLimit,
                            Window = TimeSpan.FromSeconds(settings.ImportExport.WindowSeconds),
                            QueueLimit = settings.ImportExport.QueueLimit
                        });
                }

                if (path.Contains("/reports/"))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        GetUserOrIpPartitionKey(httpContext, "rep"),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.Reports.PermitLimit,
                            Window = TimeSpan.FromSeconds(settings.Reports.WindowSeconds),
                            QueueLimit = settings.Reports.QueueLimit
                        });
                }

                if (method is "POST" or "PUT" or "DELETE" or "PATCH")
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        GetUserOrIpPartitionKey(httpContext, "crud"),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.Crud.PermitLimit,
                            Window = TimeSpan.FromSeconds(settings.Crud.WindowSeconds),
                            QueueLimit = settings.Crud.QueueLimit
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    GetUserOrIpPartitionKey(httpContext, "gen"),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.General.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.General.WindowSeconds),
                        QueueLimit = settings.General.QueueLimit
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Applies rate limiting middleware into the ASP.NET Core request pipeline.
    /// </summary>
    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetService<RateLimitSettings>();
        if (settings?.EnableRateLimiting == false)
        {
            return app;
        }

        return app.UseRateLimiter();
    }

    /// <summary>
    /// Safely extracts the client IP address considering reverse proxy headers.
    /// </summary>
    public static string GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header for client IP when behind reverse proxy
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var clientIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(clientIp) && IPAddress.TryParse(clientIp, out _))
            {
                return clientIp;
            }
        }

        // Check X-Real-IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp) && IPAddress.TryParse(realIp.Trim(), out _))
        {
            return realIp.Trim();
        }

        // Fallback to direct connection IP
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            return remoteIp;
        }

        return "unknown_ip";
    }

    /// <summary>
    /// Returns a partition key using the authenticated User ID if available, or client IP if anonymous.
    /// </summary>
    public static string GetUserOrIpPartitionKey(HttpContext context, string prefix)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("userId")?.Value
                ?? user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"{prefix}_user:{userId}";
            }
        }

        return $"{prefix}_ip:{GetClientIpAddress(context)}";
    }

    private static string GetPartitionKey(HttpContext context, string prefix)
    {
        return GetUserOrIpPartitionKey(context, prefix);
    }

    /// <summary>
    /// Identifies whether the request is exempt from rate limiting (e.g., OPTIONS, Health Check, Hangfire, Static Assets).
    /// </summary>
    public static bool IsExemptRequest(HttpContext context)
    {
        // 1. CORS Preflight requests
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            return true;
        }

        // 2. Health checks, Hangfire dashboard, Swagger UI, static files
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (path.StartsWith("/health") ||
            path.StartsWith("/hangfire") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/user_images/") ||
            path.EndsWith(".ico") ||
            path.EndsWith(".png") ||
            path.EndsWith(".jpg") ||
            path.EndsWith(".jpeg") ||
            path.EndsWith(".css") ||
            path.EndsWith(".js"))
        {
            return true;
        }

        // 3. Explicit [DisableRateLimiting] attribute
        var endpoint = context.GetEndpoint();
        if (endpoint != null && endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>() != null)
        {
            return true;
        }

        return false;
    }
}
