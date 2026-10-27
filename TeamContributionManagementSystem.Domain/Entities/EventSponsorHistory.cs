namespace TeamContributionManagementSystem.Domain.Entities;

public class EventSponsorHistory
{
    public Guid HistoryId { get; set; } = Guid.NewGuid();
    public Guid SponsorId { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Guid SponsorUserId
    {
        get => UserId;
        set => UserId = value;
    }

    public decimal? PreviousAmount { get; set; }
    public decimal ContributionAmount { get; set; }
    public string ChangeType { get; set; } = "CREATED"; // "CREATED", "UPDATED", "DEACTIVATED"
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public Guid? ActionBy { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public EventSponsor? Sponsor { get; set; }
    public Event? Event { get; set; }
    public AppUser? SponsorUser { get; set; }
    public AppUser? ActionByUser { get; set; }
}
