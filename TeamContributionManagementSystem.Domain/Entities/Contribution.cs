using System.ComponentModel.DataAnnotations.Schema;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Contribution
{
    public Guid ContributionId { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? CashAmount { get; set; }
    public decimal? UpiAmount { get; set; }

    // Foreign Keys to normalized tables (live DB columns)
    public Guid? StatusId { get; set; }
    public Status? StatusItem { get; set; }

    public Guid? PaymentModeId { get; set; }
    public PaymentModeItem? PaymentModeItem { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    // Unmapped properties for backward compatibility
    [NotMapped]
    public Guid MemberId
    {
        get => UserId;
        set => UserId = value;
    }

    [NotMapped]
    private PaymentStatus? _paymentStatus;
    [NotMapped]
    public PaymentStatus PaymentStatus
    {
        get
        {
            if (_paymentStatus.HasValue) return _paymentStatus.Value;
            if (StatusItem != null && StatusItem.StatusName.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                return PaymentStatus.Paid;
            return PaymentStatus.Pending;
        }
        set => _paymentStatus = value;
    }

    [NotMapped]
    private PaymentMode? _paymentMode;
    [NotMapped]
    public PaymentMode PaymentMode
    {
        get
        {
            if (_paymentMode.HasValue) return _paymentMode.Value;
            if (PaymentModeItem != null)
            {
                if (PaymentModeItem.IsCash) return PaymentMode.Cash;
                if (string.Equals(PaymentModeItem.PaymentType, "Split", StringComparison.OrdinalIgnoreCase)) return PaymentMode.Split;
                return PaymentMode.Upi;
            }
            return PaymentMode.None;
        }
        set => _paymentMode = value;
    }

    [NotMapped]
    public Member? Member { get; set; }

    // Common Audit Properties
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
