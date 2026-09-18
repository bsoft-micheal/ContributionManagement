using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.DTOs.Events;

public class EventSummaryDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public EventStatus Status { get; set; }
    public decimal BaseAmount { get; set; }
    public int ParticipantCount { get; set; }
    public decimal TotalExpectedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public IReadOnlyCollection<EventParticipantDto> Participants { get; set; } = Array.Empty<EventParticipantDto>();
}

public class EventParticipantDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class EventDetailsDto : EventSummaryDto
{
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public IReadOnlyCollection<Contributions.ContributionDto> Contributions { get; set; } = Array.Empty<Contributions.ContributionDto>();
}

public class ContributionOverrideDto
{
    [Required]
    public Guid MemberId { get; set; }

    [Range(0, 1000000)]
    public decimal Amount { get; set; }
}

public class CreateEventRequestDto
{
    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public Guid EventTypeId { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public EventStatus Status { get; set; } = EventStatus.Planned;

    [Range(1, 1000000, ErrorMessage = "Base amount must be greater than 0.")]
    public decimal BaseAmount { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
    public List<ContributionOverrideDto> ContributionOverrides { get; set; } = new();
}
