namespace TeamContributionManagementSystem.Application.DTOs.ExitClearance;

public class ExitClearanceSummaryDto
{
    public Guid ClearanceId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string? MemberCode { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public DateTime? JoiningDate { get; set; }
    public DateTime? PlannedExitDate { get; set; }

    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }

    public Guid? UnpaidEventId { get; set; }
    public string? UnpaidEventName { get; set; }
    public IReadOnlyCollection<EventDueItemDto> UnpaidEvents { get; set; } = new List<EventDueItemDto>();

    public decimal TotalObligations { get; set; }
    public decimal TotalVerifiedPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int PendingObligationsCount { get; set; }
    public bool HasUnverifiedPayments { get; set; }
    public string PaymentVerificationStatus { get; set; } = "All Verified";

    public string ClearanceStatus { get; set; } = "PendingClearance";
    public bool CanApprove { get; set; }
    public string? ApprovalBlockReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
}

public class ExitClearanceKpiDto
{
    public int PendingExitRequests { get; set; }
    public int MembersWithOutstandingDues { get; set; }
    public decimal TotalOutstandingAmount { get; set; }
    public int ReadyForFinalApproval { get; set; }
}

public class ExitClearanceQueueResponseDto
{
    public IReadOnlyCollection<ExitClearanceSummaryDto> Items { get; set; } = new List<ExitClearanceSummaryDto>();
    public int TotalCount { get; set; }
    public ExitClearanceKpiDto Kpis { get; set; } = new ExitClearanceKpiDto();
}

public class ExitClearanceDuesDetailDto
{
    public Guid ClearanceId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberCode { get; set; }
    public string MemberEmail { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime? JoiningDate { get; set; }

    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime? PlannedExitDate { get; set; }
    public string ClearanceStatus { get; set; } = "PendingClearance";

    public decimal TotalObligations { get; set; }
    public decimal TotalVerifiedPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public bool HasUnverifiedPayments { get; set; }
    public bool CanApprove { get; set; }
    public string? ApprovalBlockReason { get; set; }

    public IReadOnlyCollection<EventDueItemDto> EventDues { get; set; } = new List<EventDueItemDto>();
    public IReadOnlyCollection<ExitPaymentHistoryItemDto> PaymentHistory { get; set; } = new List<ExitPaymentHistoryItemDto>();
}

public class EventDueItemDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal ContributionAmount { get; set; }
    public decimal VerifiedPaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public bool HasPendingPayment { get; set; }
    public decimal PendingPaymentAmount { get; set; }
}

public class ExitPaymentHistoryItemDto
{
    public Guid TransactionId { get; set; }
    public string TxnNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public Guid? EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Utr { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public string? Notes { get; set; }
}

public class ApproveExitRequestDto
{
    public string? ApprovalRemarks { get; set; }
}

public class ExitClearanceHistoryDto
{
    public Guid HistoryId { get; set; }
    public Guid ClearanceId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberNameSnapshot { get; set; } = string.Empty;
    public string? MemberCodeSnapshot { get; set; }
    public Guid EventId { get; set; }
    public string EventNameSnapshot { get; set; } = string.Empty;

    public DateTime? JoiningDate { get; set; }
    public DateTime ExitApprovalDate { get; set; }

    public Guid ApprovedBy { get; set; }
    public string ApprovedByName { get; set; } = string.Empty;

    public decimal TotalObligations { get; set; }
    public decimal TotalVerifiedPaid { get; set; }
    public decimal TotalApprovedAdjustments { get; set; }
    public decimal OutstandingAtApproval { get; set; }

    public string? PaymentVerificationSummary { get; set; }
    public string? ApprovalRemarks { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
