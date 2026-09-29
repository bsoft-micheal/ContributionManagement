namespace TeamContributionManagementSystem.Domain.Entities;

public class PaymentModeItem
{
    public Guid PaymentModeId { get; set; }
    public string PaymentModeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public bool IsCash { get; set; } = false;
    public bool SupportsQr { get; set; } = true;
    public string? PaymentType { get; set; } = "Digital";

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
