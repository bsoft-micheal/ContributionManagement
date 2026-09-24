namespace TeamContributionManagementSystem.Domain.Entities;

public class Role
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal DefaultContributionAmount { get; set; }

    public ICollection<Member> Members { get; set; } = new List<Member>();
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
