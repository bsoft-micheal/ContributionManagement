using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

public class BudgetCalculationHistory
{
    public Guid HistoryId { get; set; } = Guid.NewGuid();
    public Guid BudgetCalculationId { get; set; }
    public decimal? PreviousRate { get; set; }
    public decimal NewRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string ChangeType { get; set; } = "INITIAL"; // INITIAL, INCREASE, DECREASE
    public decimal? ChangeAmount { get; set; }
    public decimal? ChangePercentage { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedOn { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public BudgetCalculation? BudgetCalculation { get; set; }
    public AppUser? ChangedByUser { get; set; }
}
