namespace TeamContributionManagementSystem.Application.Common;

/// <summary>
/// Centralized role names and authorization policies.
/// </summary>
public static class CommonRoles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Member = "Member";

    public const string AdminOrOrganizer = $"{Admin},{Organizer}";
}
