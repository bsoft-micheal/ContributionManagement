namespace TeamContributionManagementSystem.Application.DTOs.Settings;

public class SettingItemDto
{
    public Guid SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SystemSettingsDto
{
    // General
    public string OrgName { get; set; } = "Unit 1A Residents Association";
    public bool BirthdayMembersExempt { get; set; } = true;
    public string DefaultCurrency { get; set; } = "INR";
    public string TimeZone { get; set; } = "Asia/Kolkata";

    // Email
    public string FromEmail { get; set; } = "noreply@unit1a.com";
    public string FromName { get; set; } = "Unit 1A Management";
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public string SmtpPort { get; set; } = "587";
    public string Encryption { get; set; } = "TLS";

    // Security / OTP
    public string OtpExpiry { get; set; } = "10";
    public string MaxRetry { get; set; } = "3";
    public bool EnableOtpLogin { get; set; } = true;
    public bool Enable2faAdmin { get; set; } = false;

    // Notifications
    public bool EnableEmailNotif { get; set; } = true;
    public bool NotifNewMember { get; set; } = true;
    public bool NotifPaymentConfirm { get; set; } = true;
    public bool NotifEventReminder { get; set; } = true;
    public bool NotifSupportTicket { get; set; } = false;

    // Payment QR
    public string QrReceiverName { get; set; } = "Daniel A";
    public string QrUpiId { get; set; } = "danielrobertanto604@okicici";
    public string QrImage { get; set; } = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=upi://pay?pa=danielrobertanto604@okicici%26pn=Daniel%20A";

    // Audit
    public bool EnableAuditLogs { get; set; } = true;
    public bool LogUserLogin { get; set; } = true;
    public bool LogDataChanges { get; set; } = true;
    public bool LogConfigChanges { get; set; } = true;
    public string RetentionPeriod { get; set; } = "365";
}
