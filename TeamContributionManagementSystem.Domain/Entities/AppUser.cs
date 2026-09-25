using System.ComponentModel.DataAnnotations.Schema;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class AppUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ProfileImage { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? PasswordResetOtp { get; set; }
    public DateTime? PasswordResetOtpExpiry { get; set; }

    public bool IsTwoFactorEnabled { get; set; } = false;

    public ICollection<UserMfaDevice> MfaDevices { get; set; } = new List<UserMfaDevice>();

    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();

    /// <summary>
    /// Linked Member records (matched by Email).
    /// Kept unmapped to prevent EF Core from issuing ALTER TABLE / DB foreign key constraints.
    /// </summary>
    [NotMapped]
    public ICollection<Member> Members { get; set; } = new List<Member>();

    /// <summary>
    /// Convenience accessor for the primary linked member profile.
    /// </summary>
    [NotMapped]
    public Member? MemberProfile => Members?.FirstOrDefault();
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

