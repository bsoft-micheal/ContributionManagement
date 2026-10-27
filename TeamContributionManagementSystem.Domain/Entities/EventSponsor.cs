namespace TeamContributionManagementSystem.Domain.Entities;

public class EventSponsor
{
    public Guid SponsorId { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Guid SponsorUserId
    {
        get => UserId;
        set => UserId = value;
    }

    public decimal ContributionAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public Event? Event { get; set; }
    public AppUser? SponsorUser { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public AppUser? ModifiedByUser { get; set; }
    public ICollection<EventSponsorHistory> Histories { get; set; } = new List<EventSponsorHistory>();
}
