namespace TeamContributionManagementSystem.Domain.Entities;

public class EventType
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public decimal BaseAmount { get; set; }

    // Calculation Rule Properties (Configured in Support Data)
    public bool HasTenureRule { get; set; } = false;
    public decimal TenureThresholdYears { get; set; } = 1.0m;
    public decimal NewEntrantSharePercentage { get; set; } = 50.0m;
    public decimal StandardSharePercentage { get; set; } = 100.0m;
    public string? RuleDescription { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
