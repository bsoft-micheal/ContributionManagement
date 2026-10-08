using System.ComponentModel.DataAnnotations;
using TeamContributionManagementSystem.Application.Common;

namespace TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;

public class BudgetCalculationDto
{
    public Guid BudgetCalculationId { get; set; }
    public Guid? EventTypeId { get; set; }
    public string ExpenseItem { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsReferred { get; set; }
}

public class CreateBudgetCalculationRequestDto
{
    [Required(ErrorMessage = CommonValidationMessages.ExpenseItemRequired)]
    [MaxLength(150)]
    public string ExpenseItem { get; set; } = string.Empty;

    [Range(1, 1000000, ErrorMessage = CommonValidationMessages.RateRange)]
    public decimal Rate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public Guid? EventTypeId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBudgetCalculationRequestDto : CreateBudgetCalculationRequestDto
{
    public DateTime? EffectiveFrom { get; set; }
    public string? ChangeReason { get; set; }
    public string? Remarks { get; set; }
}

public class BudgetCalculationHistoryDto
{
    public Guid HistoryId { get; set; }
    public Guid BudgetCalculationId { get; set; }
    public decimal? PreviousRate { get; set; }
    public decimal NewRate { get; set; }
    public string ChangeType { get; set; } = string.Empty; // INITIAL, INCREASE, DECREASE
    public decimal? ChangeAmount { get; set; }
    public decimal? ChangePercentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public string? ChangedBy { get; set; }
    public Guid? ChangedById { get; set; }
    public DateTime ChangedOn { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateBudgetCalculationRateRequestDto
{
    [Required(ErrorMessage = "New rate is required.")]
    [Range(0, 10000000, ErrorMessage = "New rate must be between 0 and 10,000,000.")]
    public decimal NewRate { get; set; }

    [Required(ErrorMessage = "Effective from date is required.")]
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;

    [Required(ErrorMessage = "Reason for rate change is required.")]
    [MinLength(5, ErrorMessage = "Reason must be at least 5 characters.")]
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string ChangeReason { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters.")]
    public string? Remarks { get; set; }
}

