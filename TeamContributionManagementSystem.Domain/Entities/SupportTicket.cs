using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class SupportTicket
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;

    // Foreign Keys to normalized tables (live DB columns)
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public Guid? TicketTypeId { get; set; }
    public TicketType? TicketTypeItem { get; set; }

    public Guid? PriorityId { get; set; }
    public Priority? PriorityItem { get; set; }

    public Guid? StatusId { get; set; }
    public Status? StatusItem { get; set; }

    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? RefNo { get; set; }
    public string? Utr { get; set; }
    public string? Attachment { get; set; }
    public string? ResolutionNotes { get; set; }

    // Unmapped properties preserved for business logic and DTO compatibility
    [NotMapped]
    private string? _memberName;
    [NotMapped]
    public string MemberName
    {
        get => !string.IsNullOrWhiteSpace(_memberName) ? _memberName : (User != null ? (!string.IsNullOrWhiteSpace(User.FullName) ? User.FullName : User.Username) : string.Empty);
        set => _memberName = value;
    }

    [NotMapped]
    private string? _memberId;
    [NotMapped]
    public string? MemberId
    {
        get => !string.IsNullOrWhiteSpace(_memberId) ? _memberId : UserId?.ToString();
        set => _memberId = value;
    }

    [NotMapped]
    private string? _relatedEvent;
    [NotMapped]
    public string? RelatedEvent
    {
        get => !string.IsNullOrWhiteSpace(_relatedEvent) ? _relatedEvent : Event?.EventName;
        set => _relatedEvent = value;
    }

    [NotMapped]
    private string? _ticketType;
    [NotMapped]
    public string TicketType
    {
        get => !string.IsNullOrWhiteSpace(_ticketType) ? _ticketType : (TicketTypeItem?.TypeName ?? string.Empty);
        set => _ticketType = value;
    }

    [NotMapped]
    private string? _priority;
    [NotMapped]
    public string Priority
    {
        get => !string.IsNullOrWhiteSpace(_priority) ? _priority : (PriorityItem?.PriorityName ?? string.Empty);
        set => _priority = value;
    }

    [NotMapped]
    private string? _status;
    [NotMapped]
    public string Status
    {
        get => !string.IsNullOrWhiteSpace(_status) ? _status : (StatusItem?.StatusName ?? string.Empty);
        set => _status = value;
    }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
