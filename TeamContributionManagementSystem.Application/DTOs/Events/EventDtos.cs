using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.DTOs.Events;

public class EventSummaryDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? EventDates { get; set; }
    public string Description { get; set; } = string.Empty;
    public EventStatus Status { get; set; }
    public decimal BaseAmount { get; set; }
    public int ParticipantCount { get; set; }
    public decimal TotalExpectedAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public int PendingContributionsCount { get; set; }
    public int PaidContributionsCount { get; set; }
    public bool HasPaidContributions { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal OverallExpectedAmount { get; set; }
    public decimal OverallExpenseAmount { get; set; }
    public decimal OverallRemainingAmount { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public string? PublishedByName { get; set; }

    // Calculation Rule Properties (inherited from EventType)
    public bool HasTenureRule { get; set; }
    public decimal TenureThresholdYears { get; set; }
    public decimal NewEntrantSharePercentage { get; set; }
    public decimal StandardSharePercentage { get; set; }
    public string? RuleDescription { get; set; }
    public bool IsReferred { get; set; }
    public int SupportTicketRaisedCount { get; set; }
    public bool UseAvailableBalance { get; set; }
    public decimal AllocatedAmount { get; set; }
    public bool UsePreviousEventBalance { get; set; }
    public decimal PreviousEventAdjustment { get; set; }
    public string? PreviousEventAdjustmentType { get; set; }
    public decimal OriginalPlannedBudget { get; set; }
    public List<Guid>? EventTypeIds { get; set; }
    public Dictionary<string, string>? EventTypeDates { get; set; }
    public Dictionary<string, List<Guid>>? EventTypeMembers { get; set; }
    public Dictionary<string, Dictionary<string, string>>? EventTypeMemberDates { get; set; }
    public bool EnableCelebrantSelection { get; set; }
    public bool RequireSponsorContribution { get; set; }
    public Guid? SponsorUserId { get; set; }
    public string? SponsorName { get; set; }
    public decimal? SponsorContributionAmount { get; set; }

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
    public IReadOnlyCollection<Contributions.ContributionDto> Contributions { get; set; } = Array.Empty<Contributions.ContributionDto>();
}

public class AvailableAmountInHandDto
{
    public decimal ActualAmountInHand { get; set; }
    public decimal TotalReservedAmount { get; set; }
    public decimal AvailableAmountInHand { get; set; }
    public decimal RawAmountInHand { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public decimal EligibleAdjustmentAmount { get; set; }
    public decimal TotalRecoveredDeficits { get; set; }
    public string AdjustmentType { get; set; } = "None"; // "DeficitRecovery", "SurplusCredit", "None"
    public string? PreviousEventName { get; set; }
    public Guid? PreviousEventId { get; set; }
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
    [MaxLength(250)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public Guid EventTypeId { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [MaxLength(500)]
    public string? EventDates { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public EventStatus Status { get; set; } = EventStatus.Planned;

    [Range(0, 1000000, ErrorMessage = "Base amount must be between 0 and 1,000,000.")]
    public decimal BaseAmount { get; set; }
    public bool UseAvailableBalance { get; set; }
    public decimal? AllocatedAmount { get; set; }
    public bool UsePreviousEventBalance { get; set; }
    public decimal? PreviousEventAdjustment { get; set; }
    public string? PreviousEventAdjustmentType { get; set; }
    public List<Guid>? EventTypeIds { get; set; }
    public Dictionary<string, string>? EventTypeDates { get; set; }
    public Dictionary<string, List<Guid>>? EventTypeMembers { get; set; }
    public Dictionary<string, Dictionary<string, string>>? EventTypeMemberDates { get; set; }
    public bool EnableCelebrantSelection { get; set; }
    public bool RequireSponsorContribution { get; set; }
    public Guid? SponsorUserId { get; set; }
    [Range(0, 1000000, ErrorMessage = "Sponsor contribution amount must be between 0 and 1,000,000.")]
    public decimal? SponsorContributionAmount { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
    public List<ContributionOverrideDto> ContributionOverrides { get; set; } = new();
}

public class EventContributionPreviewRequestDto
{
    public Guid EventTypeId { get; set; }
    public List<Guid>? EventTypeIds { get; set; }
    public DateTime EventDate { get; set; }
    public Dictionary<string, string>? EventTypeDates { get; set; }
    public Dictionary<string, List<Guid>>? EventTypeMembers { get; set; }
    public Dictionary<string, Dictionary<string, string>>? EventTypeMemberDates { get; set; }
    public bool EnableCelebrantSelection { get; set; }
    public decimal BaseAmount { get; set; }
    public bool UseAvailableBalance { get; set; }
    public decimal? AllocatedAmount { get; set; }
    public bool UsePreviousEventBalance { get; set; }
    public decimal? PreviousEventAdjustment { get; set; }
    public string? PreviousEventAdjustmentType { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
    public List<ContributionOverrideDto>? ContributionOverrides { get; set; }
}

public class EventContributionPreviewMemberDto
{
    public Guid UserId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string WorkType { get; set; } = "Office";
    public decimal CompletedYears { get; set; }
    public decimal SharePercentage { get; set; } = 100m;
    public decimal StandardAmount { get; set; }
    public decimal ContributionAmount { get; set; }
}

public class EventContributionPreviewResponseDto
{
    public int TotalMembers { get; set; }
    public int EligibleContributors { get; set; }
    public decimal PlannedBudget { get; set; }
    public decimal ExpectedCollection { get; set; }
    public decimal StandardContributionAmount { get; set; }
    public bool UsePreviousEventBalance { get; set; }
    public decimal EligibleAdjustmentAmount { get; set; }
    public string AdjustmentType { get; set; } = "None";
    public decimal EffectiveBudget { get; set; }
    public IReadOnlyCollection<EventContributionPreviewMemberDto> Members { get; set; } = Array.Empty<EventContributionPreviewMemberDto>();
}

public class MemberContributionCalculationResult
{
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    public decimal CompletedYears { get; set; }
    public decimal SharePercentage { get; set; } = 100m;
    public decimal StandardAmount { get; set; }
}

public class CelebratingMemberDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CelebrationReason { get; set; } = string.Empty;
}

public class MonthlyEventRestrictionsDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsBirthdayTaken { get; set; }
    public List<Guid> TakenEventTypeIds { get; set; } = new();
    public List<string> TakenEventTypeNames { get; set; } = new();
    public List<CelebratingMemberDto> CelebratingMembers { get; set; } = new();
}

public class EligibleSponsorDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? RoleName { get; set; }
}

public class EventSponsorDto
{
    public Guid SponsorId { get; set; }
    public Guid EventId { get; set; }
    public Guid SponsorUserId { get; set; }
    public string SponsorName { get; set; } = string.Empty;
    public string? SponsorEmail { get; set; }
    public decimal ContributionAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

public class EventSponsorHistoryDto
{
    public Guid HistoryId { get; set; }
    public Guid SponsorId { get; set; }
    public Guid EventId { get; set; }
    public Guid SponsorUserId { get; set; }
    public string SponsorName { get; set; } = string.Empty;
    public decimal? PreviousAmount { get; set; }
    public decimal ContributionAmount { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? ActionByName { get; set; }
    public string? Notes { get; set; }
}


