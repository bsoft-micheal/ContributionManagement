using System.ComponentModel.DataAnnotations;

namespace TeamContributionManagementSystem.Application.DTOs.Expenses;

public class ExpenseDto
{
    public Guid ExpenseId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string SubmittedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateExpenseRequestDto
{
    [Required]
    [MaxLength(200)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000000)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    [MaxLength(150)]
    public string SubmittedBy { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ApprovedBy { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FileName { get; set; }

    public string? FileData { get; set; }
}

public class UpdateExpenseRequestDto : CreateExpenseRequestDto
{
}
