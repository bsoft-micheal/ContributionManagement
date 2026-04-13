using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.EventTypes;

public class EventTypeDto
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateEventTypeRequestDto
{
    [Required]
    [MaxLength(100)]
    public string EventTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class UpdateEventTypeRequestDto : CreateEventTypeRequestDto
{
}
