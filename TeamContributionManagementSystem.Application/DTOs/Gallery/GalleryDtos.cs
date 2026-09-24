using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Gallery;

public class GalleryPhotoDto
{
    public Guid PhotoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime TakenDate { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateGalleryPhotoRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "Moments";

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    public DateTime TakenDate { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
