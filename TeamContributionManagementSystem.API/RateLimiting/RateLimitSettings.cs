namespace TeamContributionManagementSystem.API.RateLimiting;

/// <summary>
/// Strongly-typed configuration options for API Rate Limiting.
/// Mapped from the "RateLimiting" section in appsettings.json.
/// </summary>
public class RateLimitSettings
{
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Authentication/Login policy (Default: 5 requests per minute, strict IP-based).
    /// </summary>
    public PolicySettings Auth { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    /// <summary>
    /// General API endpoints (Default: 100 requests per minute).
    /// </summary>
    public PolicySettings General { get; set; } = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    /// <summary>
    /// Standard CRUD operations (Default: 60 requests per minute).
    /// </summary>
    public PolicySettings Crud { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    /// <summary>
    /// Reporting/Aggregation endpoints (Default: 10 requests per minute).
    /// </summary>
    public PolicySettings Reports { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    /// <summary>
    /// Bulk Import / Export operations (Default: 5 requests per minute).
    /// </summary>
    public PolicySettings ImportExport { get; set; } = new()
    {
        PermitLimit = 5,
        WindowSeconds = 60,
        QueueLimit = 0
    };
}

/// <summary>
/// Parameters governing a specific rate limiting policy.
/// </summary>
public class PolicySettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}
