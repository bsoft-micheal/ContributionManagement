using System.ComponentModel.DataAnnotations.Schema;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class AppUser
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [NotMapped]
    public UserRole Role
    {
        get
        {
            var roleName = UserRoles?.FirstOrDefault()?.Role?.RoleName;
            if (!string.IsNullOrEmpty(roleName) && Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var r))
            {
                return r;
            }
            return UserRole.Member;
        }
        set { }
    }

    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
    public string FullName { get; set; } = string.Empty;

    [NotMapped]
    public string Name
    {
        get => FullName;
        set => FullName = value;
    }
    public bool IsActive { get; set; } = true;
    public string? ProfileImage { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? PasswordResetOtp { get; set; }
    public DateTime? PasswordResetOtpExpiry { get; set; }

    public bool IsTwoFactorEnabled { get; set; } = false;
    public bool IsFirstLogin { get; set; } = true;

    // Member profile fields merged directly into AppUser
    public string Phone { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public Guid? WorkTypeId { get; set; }
    public WorkType? WorkTypeNavigation { get; set; }

    private string? _workType;

    [NotMapped]
    public string WorkType
    {
        get => !string.IsNullOrWhiteSpace(_workType) ? _workType : (WorkTypeNavigation?.WorkTypeName ?? string.Empty);
        set => _workType = value;
    }
    [NotMapped]
    public Guid? RoleId
    {
        get => UserRoles?.FirstOrDefault()?.RoleId;
        set
        {
            if (value.HasValue)
            {
                var existing = UserRoles?.FirstOrDefault();
                if (existing != null)
                {
                    existing.RoleId = value.Value;
                }
                else if (UserRoles != null && !UserRoles.Any(ur => ur.RoleId == value.Value))
                {
                    UserRoles.Add(new AppUserRole { UserId = UserId, RoleId = value.Value });
                }
            }
        }
    }

    [NotMapped]
    public Role? RoleNavigation
    {
        get => UserRoles?.FirstOrDefault()?.Role;
        set
        {
            if (value != null)
            {
                var existing = UserRoles?.FirstOrDefault();
                if (existing != null)
                {
                    existing.RoleId = value.RoleId;
                    existing.Role = value;
                }
                else if (UserRoles != null && !UserRoles.Any(ur => ur.RoleId == value.RoleId))
                {
                    UserRoles.Add(new AppUserRole { UserId = UserId, RoleId = value.RoleId, Role = value });
                }
            }
        }
    }
    public DateTime DateOfBirth { get; set; }
    public DateTime JoiningDate { get; set; }
    public bool IsExited { get; set; } = false;

    public ICollection<UserMfaDevice> MfaDevices { get; set; } = new List<UserMfaDevice>();
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

