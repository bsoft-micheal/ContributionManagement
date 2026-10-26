using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class ExitClearance
{
    public Guid ClearanceId { get; set; }

    public Guid MemberId { get; set; }
    public AppUser? Member { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public string Status { get; set; } = "PendingClearance";
    public DateTime? PlannedExitDate { get; set; }
    public string? Notes { get; set; }
    public string? ApprovalRemarks { get; set; }

    public Guid? ApprovedBy { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    public ICollection<ExitClearanceHistory> Histories { get; set; } = new List<ExitClearanceHistory>();
}
