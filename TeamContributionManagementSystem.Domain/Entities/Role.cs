namespace TeamContributionManagementSystem.Domain.Entities;

public class Role
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal DefaultContributionAmount { get; set; }

    public ICollection<Member> Members { get; set; } = new List<Member>();
}
