using System;

namespace TeamContributionManagementSystem.Application.DTOs.Dashboard;

/// <summary>
/// Aggregated summary metrics for an authenticated member's dashboard.
/// </summary>
public class MemberDashboardSummaryDto
{
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public int TotalEvents { get; set; }
    public int PaidEventsCount { get; set; }
    public int PendingEventsCount { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int? SelectedMonth { get; set; }
    public int? SelectedYear { get; set; }
}

/// <summary>
/// Individual event-wise contribution detail item for the authenticated member.
/// </summary>
public class MemberContributionItemDto
{
    public Guid EventId { get; set; }
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime? CreatedAt { get; set; }
    public decimal ContributionAmount { get; set; }
    public decimal VerifiedPaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal PreviousArrears { get; set; }
    public decimal TotalDue { get; set; }
    public string PaymentVerificationStatus { get; set; } = "None"; // "Verified", "Pending", "Rejected", "None"
    public string PaymentStatus { get; set; } = "Unpaid"; // "Unpaid", "Partially Paid", "Pending Verification", "Paid", "Payment Rejected"
    public bool PaymentEligibility { get; set; } = true;
    public Guid? ContributionId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
}

/// <summary>
/// Recent payment transaction executed by the authenticated member.
/// </summary>
public class MemberRecentPaymentDto
{
    public Guid PaymentId { get; set; }
    public string TxnNumber { get; set; } = string.Empty;
    public Guid? EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Pending"; // "Verified", "Pending", "Rejected"
}
