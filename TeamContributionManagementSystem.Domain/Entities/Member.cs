using TeamContributionManagementSystem.Domain.Common;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Member : IAuditableEntity
{
    public Guid MemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime JoiningDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsExited { get; set; }
    public bool IsDeleted { get; set; }
    public string MemberType { get; set; } = string.Empty;

    public Role? Role { get; set; }
    public ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

