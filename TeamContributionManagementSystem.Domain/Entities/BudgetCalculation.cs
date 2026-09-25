namespace TeamContributionManagementSystem.Domain.Entities;

public class BudgetCalculation
{
    public Guid BudgetCalculationId { get; set; }
    public string ExpenseItem { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string? Category { get; set; } = "Birthday";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
