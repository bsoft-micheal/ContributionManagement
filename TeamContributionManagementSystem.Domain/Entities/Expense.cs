using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class Expense
{
    public Guid ExpenseId { get; set; }
    public string ExpenseNo { get; set; } = string.Empty;

    [NotMapped]
    public string ExpenseNumber { get => ExpenseNo; set => ExpenseNo = value; }
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FileName { get; set; }

    public decimal BudgetAmount { get; set; } = 0m;
    public decimal ExtraAmount { get; set; } = 0m;
    public string? AdditionalExpenses { get; set; }

    // Default Audit Fields
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
