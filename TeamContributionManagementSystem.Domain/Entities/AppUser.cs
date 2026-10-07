using System.ComponentModel.DataAnnotations.Schema;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class AppUser
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
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
    public bool IsPrimary { get; set; } = false;
    public bool IsSecondary { get; set; } = false;
    public bool EnableMultipleRoles { get; set; } = false;

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
    private Guid? _activeRoleId;

    [NotMapped]
    public Guid? RoleId
    {
        get => _activeRoleId ?? UserRoles?.FirstOrDefault(ur => ur.IsPrimary)?.RoleId ?? UserRoles?.FirstOrDefault()?.RoleId;
        set => _activeRoleId = value;
    }

    [NotMapped]
    public Role? RoleNavigation
    {
        get => UserRoles?.FirstOrDefault(ur => ur.IsPrimary)?.Role ?? UserRoles?.FirstOrDefault()?.Role;
        set
        {
            if (value != null)
            {
                _activeRoleId = value.RoleId;
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
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

