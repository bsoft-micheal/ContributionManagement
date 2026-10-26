namespace TeamContributionManagementSystem.Domain.Entities;

public class ExitClearanceHistory
{
    public Guid HistoryId { get; set; }

    public Guid ClearanceId { get; set; }
    public ExitClearance? Clearance { get; set; }

    public Guid MemberId { get; set; }
    public AppUser? Member { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public string MemberNameSnapshot { get; set; } = string.Empty;
    public string? MemberCodeSnapshot { get; set; }
    public string EventNameSnapshot { get; set; } = string.Empty;

    public DateTime? JoiningDate { get; set; }
    public DateTime ExitApprovalDate { get; set; } = DateTime.UtcNow;

    public Guid ApprovedBy { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public string ApprovedByName { get; set; } = string.Empty;

    public decimal TotalObligations { get; set; }
    public decimal TotalVerifiedPaid { get; set; }
    public decimal TotalApprovedAdjustments { get; set; }
    public decimal OutstandingAtApproval { get; set; }

    public string? PaymentVerificationSummary { get; set; }
    public string? ApprovalRemarks { get; set; }
    public string? SnapshotJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
