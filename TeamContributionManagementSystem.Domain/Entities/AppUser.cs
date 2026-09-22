using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class AppUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ProfileImage { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? PasswordResetOtp { get; set; }
    public DateTime? PasswordResetOtpExpiry { get; set; }

    public bool IsTwoFactorEnabled { get; set; } = false;

    public ICollection<UserMfaDevice> MfaDevices { get; set; } = new List<UserMfaDevice>();

    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
}
