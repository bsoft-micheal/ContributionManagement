using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.SupportTickets;

public class SupportTicketDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? MemberId { get; set; }
    public string? RelatedEvent { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Medium";
    public string? AssignedTo { get; set; }
    public string? RefNo { get; set; }
    public string? Utr { get; set; }
    public string? Attachment { get; set; }
    public string? ResolutionNotes { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateSupportTicketRequestDto
{
    [Required]
    [MaxLength(150)]
    public string MemberName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MemberId { get; set; }

    [MaxLength(200)]
    public string? RelatedEvent { get; set; }

    [Required]
    [MaxLength(100)]
    public string TicketType { get; set; } = "General Query";

    [Required]
    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Priority { get; set; } = "Medium";

    [MaxLength(150)]
    public string? AssignedTo { get; set; }

    [MaxLength(100)]
    public string? RefNo { get; set; }

    [MaxLength(100)]
    public string? Utr { get; set; }

    public string? Attachment { get; set; }
}

public class UpdateSupportTicketRequestDto
{
    [MaxLength(150)]
    public string? MemberName { get; set; }

    [MaxLength(100)]
    public string? MemberId { get; set; }

    [MaxLength(200)]
    public string? RelatedEvent { get; set; }

    [MaxLength(100)]
    public string? TicketType { get; set; }

    [MaxLength(300)]
    public string? Subject { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Priority { get; set; }

    [MaxLength(150)]
    public string? AssignedTo { get; set; }

    [MaxLength(100)]
    public string? RefNo { get; set; }

    [MaxLength(100)]
    public string? Utr { get; set; }

    public string? Attachment { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }
}

public class ReplyTicketRequestDto
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Status { get; set; }
}
