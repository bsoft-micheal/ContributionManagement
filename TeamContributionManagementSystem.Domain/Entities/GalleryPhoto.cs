using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class GalleryPhoto
{
    [Column("photo_id")]
    public Guid PhotoId { get; set; }

    [Column("event_id")]
    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public string Title { get; set; } = string.Empty;

    private string? _eventName;
    [NotMapped]
    public string EventName
    {
        get => !string.IsNullOrWhiteSpace(_eventName) ? _eventName : (Event?.EventName ?? string.Empty);
        set => _eventName = value;
    }

    private string? _category;
    [NotMapped]
    public string Category
    {
        get => !string.IsNullOrWhiteSpace(_category) ? _category : (Event?.EventType?.EventTypeName ?? string.Empty);
        set => _category = value;
    }

    [Column("image_url")]
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
