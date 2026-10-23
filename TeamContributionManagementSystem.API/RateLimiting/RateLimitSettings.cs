namespace TeamContributionManagementSystem.API.RateLimiting;

/// <summary>
/// Strongly-typed configuration options for API Rate Limiting.
/// Mapped from the "RateLimiting" section in appsettings.json.
/// </summary>
public class RateLimitSettings
{
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Authentication/Login/OTP policy (Default: 10 requests per minute, strict IP-based).
    /// </summary>
    public PolicySettings Auth { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60,
        QueueLimit = 2
    };

    /// <summary>
    /// General API endpoints & navigation (Default: 300 requests per minute).
    /// </summary>
    public PolicySettings General { get; set; } = new()
    {
        PermitLimit = 300,
        WindowSeconds = 60,
        QueueLimit = 20
    };

    /// <summary>
    /// Standard CRUD operations (Default: 120 requests per minute).
    /// </summary>
    public PolicySettings Crud { get; set; } = new()
    {
        PermitLimit = 120,
        WindowSeconds = 60,
        QueueLimit = 10
    };

    /// <summary>
    /// Reporting/Aggregation endpoints (Default: 60 requests per minute).
    /// </summary>
    public PolicySettings Reports { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 5
    };

    /// <summary>
    /// Bulk Import / Export operations (Default: 15 requests per minute).
    /// </summary>
    public PolicySettings ImportExport { get; set; } = new()
    {
        PermitLimit = 15,
        WindowSeconds = 60,
        QueueLimit = 2
    };

    /// <summary>
    /// Payment submissions and proof uploads (Default: 30 requests per minute).
    /// </summary>
    public PolicySettings Payments { get; set; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60,
        QueueLimit = 5
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
