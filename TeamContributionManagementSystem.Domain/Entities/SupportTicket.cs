namespace TeamContributionManagementSystem.Domain.Entities;

public class SupportTicket
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? MemberId { get; set; }
    public string? RelatedEvent { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? RefNo { get; set; }
    public string? Utr { get; set; }
    public string? Attachment { get; set; }
    public string? ResolutionNotes { get; set; }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
