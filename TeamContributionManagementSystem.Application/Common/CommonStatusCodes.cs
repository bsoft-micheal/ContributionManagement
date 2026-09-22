namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized HTTP status codes to prevent hardcoded status code values across the application.
/// </summary>
public static class CommonStatusCodes
{
    public const int Status200OK = 200;
    public const int Status201Created = 201;
    public const int Status202Accepted = 202;
    public const int Status204NoContent = 204;
    public const int Status400BadRequest = 400;
    public const int Status401Unauthorized = 401;
    public const int Status403Forbidden = 403;
    public const int Status404NotFound = 404;
    public const int Status409Conflict = 409;
    public const int Status500InternalServerError = 500;
}
