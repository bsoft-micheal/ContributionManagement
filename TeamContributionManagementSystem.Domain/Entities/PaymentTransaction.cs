using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class PaymentTransaction
{
    public Guid TransactionId { get; set; }
    public string TxnNumber { get; set; } = string.Empty;

    // Foreign Keys to normalized tables (live DB columns)
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public Guid? PaymentModeId { get; set; }
    public PaymentModeItem? PaymentModeItem { get; set; }

    public Guid? StatusId { get; set; }
    public Status? StatusItem { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Utr { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public string? Notes { get; set; }
    public string? Screenshot { get; set; }

    // Unmapped properties preserved for business logic and DTO compatibility
    [NotMapped]
    private string? _memberName;
    [NotMapped]
    public string MemberName
    {
        get => !string.IsNullOrWhiteSpace(_memberName) ? _memberName : (User != null ? (!string.IsNullOrWhiteSpace(User.FullName) ? User.FullName : User.Username) : string.Empty);
        set => _memberName = value;
    }

    [NotMapped]
    private string? _eventName;
    [NotMapped]
    public string EventName
    {
        get => !string.IsNullOrWhiteSpace(_eventName) ? _eventName : (Event != null ? Event.EventName : string.Empty);
        set => _eventName = value;
    }

    [NotMapped]
    private string? _paymentMode;
    [NotMapped]
    public string PaymentMode
    {
        get => !string.IsNullOrWhiteSpace(_paymentMode) ? _paymentMode : (PaymentModeItem != null ? PaymentModeItem.PaymentModeName : string.Empty);
        set => _paymentMode = value;
    }

    [NotMapped]
    private string? _status;
    [NotMapped]
    public string Status
    {
        get => !string.IsNullOrWhiteSpace(_status) ? _status : (StatusItem != null ? StatusItem.StatusName : string.Empty);
        set => _status = value;
    }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
