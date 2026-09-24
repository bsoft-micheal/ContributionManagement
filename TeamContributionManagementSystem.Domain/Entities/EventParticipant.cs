namespace TeamContributionManagementSystem.Domain.Entities;

public class EventParticipant
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }

    public Event? Event { get; set; }
    public Member? Member { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
