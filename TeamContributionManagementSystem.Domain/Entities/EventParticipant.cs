namespace TeamContributionManagementSystem.Domain.Entities;

public class EventParticipant
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Guid MemberId
    {
        get => UserId;
        set => UserId = value;
    }

    public Event? Event { get; set; }
    public AppUser? Member { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
