using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("ticket_resolution_notes")]
public class TicketResolutionNote
{
    public Guid NoteId { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public Guid? StatusId { get; set; }
    public Status? StatusItem { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
