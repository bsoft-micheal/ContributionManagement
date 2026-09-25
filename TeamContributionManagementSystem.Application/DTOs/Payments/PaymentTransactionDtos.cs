using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Payments;

public class PaymentTransactionDto
{
    public Guid TransactionId { get; set; }
    public string TxnNumber { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string? Utr { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public string? Notes { get; set; }
    public string? Screenshot { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreatePaymentTransactionRequestDto
{
    [Required]
    [MaxLength(150)]
    public string MemberName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000000)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string PaymentMode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Utr { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Screenshot { get; set; }
}

public class VerifyPaymentRequestDto
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Verified, Rejected, Pending

    [MaxLength(150)]
    public string? VerifiedBy { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class SubmitPaymentProofDto
{
    public Guid? EventId { get; set; }
    public Guid? MemberId { get; set; }

    [Required]
    [MaxLength(150)]
    public string MemberName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMode { get; set; } = "UPI";

    [Required]
    [MaxLength(100)]
    public string Utr { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public string? Screenshot { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class PaymentContextDto
{
    public Guid? EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid? MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string QrReceiverName { get; set; } = string.Empty;
    public string QrUpiId { get; set; } = string.Empty;
    public string QrImage { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

