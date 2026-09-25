using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;

public class BudgetCalculationDto
{
    public Guid BudgetCalculationId { get; set; }
    public string ExpenseItem { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateBudgetCalculationRequestDto
{
    [Required(ErrorMessage = "Expense Item is required.")]
    [MaxLength(150)]
    public string ExpenseItem { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "Rate must be between 0 and 1,000,000.")]
    public decimal Rate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateBudgetCalculationRequestDto : CreateBudgetCalculationRequestDto
{
}
