using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.SupportTickets;

public class SupportTicketDto
{
    public Guid TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? MemberId { get; set; }
    public Guid? EventId { get; set; }
    public string? RelatedEvent { get; set; }
    public string TicketType { get; set; } = string.Empty;
    public Guid? TicketTypeId { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? RefNo { get; set; }
    public string? Utr { get; set; }
    public string? Attachment { get; set; }
    public string? ResolutionNotes { get; set; }
    public List<TicketResolutionNoteDto> ResolutionNotesHistory { get; set; } = new();
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string? VerifiedBy { get; set; }
}

public class TicketResolutionNoteDto
{
    public Guid NoteId { get; set; }
    public Guid TicketId { get; set; }
    public Guid? UserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? StatusId { get; set; }
    public string? StatusName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupportTicketRequestDto
{
    [Required]
    [MaxLength(150)]
    public string MemberName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MemberId { get; set; }

    public Guid? EventId { get; set; }

    [MaxLength(200)]
    public string? RelatedEvent { get; set; }

    [Required]
    [MaxLength(100)]
    public string TicketType { get; set; } = string.Empty;

    public Guid? TicketTypeId { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string Priority { get; set; } = string.Empty;

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

    public Guid? EventId { get; set; }

    [MaxLength(200)]
    public string? RelatedEvent { get; set; }

    [MaxLength(100)]
    public string? TicketType { get; set; }

    public Guid? TicketTypeId { get; set; }

    [MaxLength(300)]
    public string? Subject { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [MaxLength(50)]
    public string? Priority { get; set; }

    [MaxLength(100)]
    public string? RefNo { get; set; }

    [MaxLength(100)]
    public string? Utr { get; set; }

    public string? Attachment { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    public DateTime? ModifiedOn { get; set; }
}

public class ReplyTicketRequestDto
{
    [Required]
    [MaxLength(250, ErrorMessage = CommonValidationMessages.ReplyMessageMaxLength)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Status { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
