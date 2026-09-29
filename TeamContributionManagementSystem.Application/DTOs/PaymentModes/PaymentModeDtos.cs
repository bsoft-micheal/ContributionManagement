using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.PaymentModes;

public class PaymentModeDto
{
    public Guid PaymentModeId { get; set; }
    public string PaymentModeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCash { get; set; }
    public bool SupportsQr { get; set; } = true;
    public string? PaymentType { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }

    public string? UpdatedBy
    {
        get => ModifiedBy;
        set => ModifiedBy = value;
    }

    public DateTime? UpdatedOn
    {
        get => ModifiedOn;
        set => ModifiedOn = value;
    }
}

public class CreatePaymentModeRequestDto
{
    [Required(ErrorMessage = "Payment mode name is required.")]
    [MaxLength(100, ErrorMessage = "Payment mode name cannot exceed 100 characters.")]
    public string PaymentModeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool? IsCash { get; set; }
    public bool? SupportsQr { get; set; }
    public string? PaymentType { get; set; }
}

public class UpdatePaymentModeRequestDto : CreatePaymentModeRequestDto
{
}
