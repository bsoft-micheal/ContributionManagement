using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Event
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid EventTypeId { get; set; }
    public DateTime EventDate { get; set; }
    public Guid CreatedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public EventStatus Status { get; set; } = EventStatus.Planned;
    public bool IsDeleted { get; set; }

    public EventType? EventType { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    public ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
