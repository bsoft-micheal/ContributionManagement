namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized role names and authorization policies.
/// </summary>
public static class CommonRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string Member = "Member";

    public const string AdminOrManager = $"{Admin},{Manager}";
}
