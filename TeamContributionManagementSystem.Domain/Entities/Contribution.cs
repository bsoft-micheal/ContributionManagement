using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Contribution
{
    public Guid ContributionId { get; set; }
    public Guid EventId { get; set; }
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaymentDate { get; set; }
    public PaymentMode PaymentMode { get; set; } = PaymentMode.None;
    public decimal? CashAmount { get; set; }
    public decimal? UpiAmount { get; set; }
    public bool IsDeleted { get; set; }

    public Event? Event { get; set; }
    public Member? Member { get; set; }
    public bool IsActive { get; set; } = true;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
