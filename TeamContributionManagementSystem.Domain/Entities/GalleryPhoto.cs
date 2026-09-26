namespace TeamContributionManagementSystem.Domain.Entities;

public class GalleryPhoto
{
    public Guid PhotoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime TakenDate { get; set; }
    public string? Description { get; set; }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
