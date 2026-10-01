namespace TeamContributionManagementSystem.Application.DTOs.Settings;

public class SettingItemDto
{
    public Guid SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class SystemSettingsDto
{
    // General
    public string OrgName { get; set; } = "Unit 1A Residents Association";
    public bool BirthdayMembersExempt { get; set; } = true;
    private bool _allowedMultipleEvent = false;
    public bool AllowedMultipleEvent
    {
        get => _allowedMultipleEvent;
        set => _allowedMultipleEvent = value;
    }
    public bool AllowMultipleEvents
    {
        get => _allowedMultipleEvent;
        set => _allowedMultipleEvent = value;
    }
    public string DefaultCurrency { get; set; } = "INR";
    public string TimeZone { get; set; } = "Asia/Kolkata";

    // Email
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public string SmtpPort { get; set; } = string.Empty;
    public string Encryption { get; set; } = string.Empty;

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

    // Category-based Email Template & Automation
    public string? EmailSubject { get; set; }
    public string? EmailDescription { get; set; }
    public string? SelectedTemplateCategoryId { get; set; }
    public object? CategoryTemplates { get; set; }
    public bool EnableMonthlyEmail { get; set; } = true;
    public bool EnableReminderEmail { get; set; } = true;
    public string ReminderIntervalDays { get; set; } = "10";
    public string ReminderIntervalValue { get; set; } = "10";
    public string ReminderIntervalUnit { get; set; } = "Days";
    public string MaxReminders { get; set; } = "3";
}
