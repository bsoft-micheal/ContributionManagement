using TeamContributionManagementSystem.Domain.Common;

namespace TeamContributionManagementSystem.Domain.Entities;

public class PaymentTransaction
{
    public Guid TransactionId { get; set; }
    public string TxnNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = DomainConstants.PaymentModes.Upi;
    public string? Utr { get; set; }
    public string Status { get; set; } = DomainConstants.PaymentStatuses.Pending;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public string? Notes { get; set; }
    public string? Screenshot { get; set; }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
