using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamContributionManagementSystem.Domain.Entities;

[Table("user_mfa_devices")]
public class UserMfaDevice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }
    public AppUser User { get; set; }

    [Column("device_label")]
    [MaxLength(100)]
    public string DeviceLabel { get; set; } = string.Empty;

    [Column("secret_key")]
    [MaxLength(100)]
    public string SecretKey { get; set; } = string.Empty;

    [Column("date_added")]
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
