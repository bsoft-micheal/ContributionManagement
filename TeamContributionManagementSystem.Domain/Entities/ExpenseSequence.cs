namespace TeamContributionManagementSystem.Domain.Entities;

public class ExpenseSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    public int LastSequence { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
