namespace TeamContributionManagementSystem.Domain.Entities;

public class EventType
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal BaseAmount { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
