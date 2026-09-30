namespace TeamContributionManagementSystem.Domain.Entities;

public class Priority
{
    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
