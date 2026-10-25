namespace TeamContributionManagementSystem.Domain.Entities;

/// <summary>
/// Durable delivery log for birthday celebration reminders to prevent duplicate notifications.
/// </summary>
public class BirthdayReminderLog
{
    public Guid ReminderLogId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of reminder: "Scheduled" (9:00 AM daily run) or "CatchUp" (for joiners after 9:00 AM).
    /// </summary>
    public string ReminderType { get; set; } = "Scheduled";

    /// <summary>
    /// Calendar date of the birthday celebration being reminded (stored as date in UTC).
    /// </summary>
    public DateTime CelebrationDate { get; set; }

    /// <summary>
    /// The user celebrating their birthday.
    /// </summary>
    public Guid CelebrantUserId { get; set; }

    /// <summary>
    /// The recipient user receiving the reminder email.
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// The recipient email address at time of dispatch.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Delivery status: "Queued", "Sent", "Failed".
    /// </summary>
    public string Status { get; set; } = "Sent";

    /// <summary>
    /// Timestamp when notification was dispatched.
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if status is Failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public AppUser? CelebrantUser { get; set; }
    public AppUser? RecipientUser { get; set; }
}
