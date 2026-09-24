using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.DTOs.Contributions;

public class ContributionDto
{
    public Guid ContributionId { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public decimal? CashAmount { get; set; }
    public decimal? UpiAmount { get; set; }
}

public class MemberContributionSummaryDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public IReadOnlyCollection<ContributionCategoryBreakdownDto> CategoryBreakdown { get; set; } = Array.Empty<ContributionCategoryBreakdownDto>();
    public IReadOnlyCollection<ContributionEventBreakdownDto> EventBreakdown { get; set; } = Array.Empty<ContributionEventBreakdownDto>();
}

public class ContributionCategoryBreakdownDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    public int EventCount { get; set; }
}

public class ContributionEventBreakdownDto
{
    public string EventName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
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
    public decimal? CashAmount { get; set; }
    public decimal? UpiAmount { get; set; }
}
