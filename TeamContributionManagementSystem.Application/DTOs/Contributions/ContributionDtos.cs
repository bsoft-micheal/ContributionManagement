using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.DTOs.Contributions;

public class ContributionDto
{
    public Guid ContributionId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
    public PaymentMode PaymentMode { get; set; }
}

public class PayContributionRequestDto
{
    [Required]
    public Guid EventId { get; set; }

    [Required]
    public Guid MemberId { get; set; }

    [Range(0, 1000000)]
    public decimal? Amount { get; set; }

    [Required]
    public PaymentMode PaymentMode { get; set; }

    public DateTime? PaymentDate { get; set; }
}
