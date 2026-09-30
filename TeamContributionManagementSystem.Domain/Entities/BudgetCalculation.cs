using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class BudgetCalculation
{
    public Guid BudgetCalculationId { get; set; }
    public Guid? EventTypeId { get; set; }
    public string ExpenseItem { get; set; } = string.Empty;
    public decimal Rate { get; set; }

    private string? _category;

    [NotMapped]
    public string? Category
    {
        get => !string.IsNullOrWhiteSpace(_category) ? _category : EventType?.EventTypeName;
        set => _category = value;
    }

    public EventType? EventType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public Guid? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
