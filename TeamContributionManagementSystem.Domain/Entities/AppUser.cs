using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class AppUser
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
}
