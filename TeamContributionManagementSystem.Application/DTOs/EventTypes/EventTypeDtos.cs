using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.EventTypes;

public class EventTypeDto
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal BaseAmount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class CreateEventTypeRequestDto
{
    [Required]
    [MaxLength(100)]
    public string EventTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, 1000000, ErrorMessage = "Base amount must be greater than 0.")]
    public decimal BaseAmount { get; set; }
}

public class UpdateEventTypeRequestDto : CreateEventTypeRequestDto
{
}
