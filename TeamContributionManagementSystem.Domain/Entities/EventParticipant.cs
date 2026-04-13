namespace TeamContributionManagementSystem.Domain.Entities;

public class EventParticipant
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }

    public Event? Event { get; set; }
    public Member? Member { get; set; }
}
