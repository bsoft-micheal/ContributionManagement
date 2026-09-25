using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.EventTypes;

public class EventTypeDto
{
    public Guid EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal BaseAmount { get; set; }

    // Calculation Rule Properties
    public bool HasTenureRule { get; set; }
    public decimal TenureThresholdYears { get; set; }
    public decimal NewEntrantSharePercentage { get; set; }
    public decimal StandardSharePercentage { get; set; }
    public string? RuleDescription { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateEventTypeRequestDto
{
    [Required]
    [MaxLength(100)]
    public string EventTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, 1000000, ErrorMessage = CommonValidationMessages.BaseAmountGreaterThanZero)]
    public decimal BaseAmount { get; set; }

    // Calculation Rule Properties
    public bool HasTenureRule { get; set; } = false;
    public decimal TenureThresholdYears { get; set; } = 1.0m;
    public decimal NewEntrantSharePercentage { get; set; } = 50.0m;
    public decimal StandardSharePercentage { get; set; } = 100.0m;

    [MaxLength(200)]
    public string? RuleDescription { get; set; }
}

public class UpdateEventTypeRequestDto : CreateEventTypeRequestDto
{
}
