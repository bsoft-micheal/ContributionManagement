using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("device_details")]
public class DeviceDetail
{
    [Key]
    [Column("device_detail_id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }
    public AppUser User { get; set; }

    [Column("device_id")]
    [MaxLength(255)]
    public string DeviceId { get; set; } 
    
    [Column("device_name")]
    [MaxLength(100)]
    public string DeviceName { get; set; }
    
    [Column("brand")]
    [MaxLength(50)]
    public string Brand { get; set; }
    
    [Column("model")]
    [MaxLength(100)]
    public string Model { get; set; }

    [Column("os")]
    [MaxLength(50)]
    public string Os { get; set; }

    [Column("os_version")]
    [MaxLength(50)]
    public string OsVersion { get; set; }

    [Column("system_name")]
    [MaxLength(50)]
    public string SystemName { get; set; }

    [Column("system_version")]
    [MaxLength(50)]
    public string SystemVersion { get; set; }

    [Column("device_type")]
    public short DeviceType { get; set; } // 1 = Web Browser, 2 = Mobile App

    [Column("app_version")]
    [MaxLength(20)]
    public string AppVersion { get; set; }

    [Column("total_memory")]
    public long? TotalMemory { get; set; }

    [Column("browser")]
    [MaxLength(100)]
    public string Browser { get; set; }

    [Column("browser_version")]
    [MaxLength(50)]
    public string BrowserVersion { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_seen_at")]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<DeviceLoginHistory> LoginHistories { get; set; } = new List<DeviceLoginHistory>();
}
