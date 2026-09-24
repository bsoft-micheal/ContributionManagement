using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("device_login_history")]
public class DeviceLoginHistory
{
    [Key]
    [Column("history_id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Column("device_detail_id")]
    public Guid DeviceDetailId { get; set; }
    public DeviceDetail? DeviceDetail { get; set; }

    [Column("login_time")]
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    [Column("logout_time")]
    public DateTime? LogoutTime { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Common Audit Properties
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
