using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Services;

/// <summary>
/// Hangfire background job service for handling automated and manual contribution reminder emails.
/// </summary>
public class ContributionReminderJobService : IContributionReminderJobService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ISystemSettingService _settingService;
    private readonly ILogger<ContributionReminderJobService> _logger;
    private readonly IConfiguration _configuration;

    public ContributionReminderJobService(
        ApplicationDbContext context,
        IEmailService emailService,
        ISystemSettingService settingService,
        ILogger<ContributionReminderJobService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _settingService = settingService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessDailyRemindersAsync(string? triggeredBy = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting automated daily contribution reminder job. TriggeredBy: {TriggeredBy}", triggeredBy ?? "Hangfire-Recurring-Schedule");

        try
        {
            var systemSettings = await GetSystemSettingsSafeAsync(cancellationToken);
            if (systemSettings != null && !systemSettings.EnableReminderEmail)
            {
                _logger.LogInformation("Contribution reminder emails are currently disabled in system settings. Skipping automated run.");
                return;
            }

            // Find all active events with status that is not Cancelled and not Completed
            var activeEvents = await _context.Events
                .Include(e => e.EventType)
                .Where(e => !e.IsDeleted && e.Status != EventStatus.Cancelled && e.Status != EventStatus.Completed)
                .OrderBy(e => e.EventDate)
                .ToListAsync(cancellationToken);

            if (activeEvents.Count == 0)
            {
                _logger.LogInformation("No active events found for contribution reminders.");
                return;
            }

            _logger.LogInformation("Found {Count} active events for contribution reminder evaluation.", activeEvents.Count);

            int totalEvaluated = 0;
            int totalSent = 0;
            int totalSkippedPaid = 0;
            int totalSkippedDuplicate = 0;
            int totalFailed = 0;

            foreach (var eventItem in activeEvents)
            {
                var result = await ProcessRemindersForEventInternalAsync(eventItem, systemSettings, triggeredBy ?? "ScheduledJob", cancellationToken);
                totalEvaluated += result.Evaluated;
                totalSent += result.Sent;
                totalSkippedPaid += result.SkippedPaid;
                totalSkippedDuplicate += result.SkippedDuplicate;
                totalFailed += result.Failed;
            }

            _logger.LogInformation(
                "Completed daily contribution reminder job. Evaluated: {Evaluated}, Sent: {Sent}, Skipped (Paid): {SkippedPaid}, Skipped (Duplicate 24h): {SkippedDuplicate}, Failed: {Failed}",
                totalEvaluated, totalSent, totalSkippedPaid, totalSkippedDuplicate, totalFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred during daily contribution reminder processing.");
            throw;
        }
    }

    /// <inheritdoc />
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task SendRemindersForEventAsync(Guid eventId, string? triggeredBy = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting manual event contribution reminder job for Event ID: {EventId}. TriggeredBy: {TriggeredBy}", eventId, triggeredBy ?? "Manual-API");

        try
        {
            var eventItem = await _context.Events
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted, cancellationToken);

            if (eventItem == null)
            {
                _logger.LogWarning("Event with ID {EventId} not found or is marked as deleted.", eventId);
                return;
            }

            var systemSettings = await GetSystemSettingsSafeAsync(cancellationToken);
            var result = await ProcessRemindersForEventInternalAsync(eventItem, systemSettings, triggeredBy ?? "Manual-Event-Trigger", cancellationToken);

            _logger.LogInformation(
                "Completed manual reminder job for Event {EventName} ({EventId}). Evaluated: {Evaluated}, Sent: {Sent}, Skipped (Paid): {SkippedPaid}, Skipped (Duplicate 24h): {SkippedDuplicate}, Failed: {Failed}",
                eventItem.EventName, eventId, result.Evaluated, result.Sent, result.SkippedPaid, result.SkippedDuplicate, result.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing manual reminder job for Event ID: {EventId}", eventId);
            throw;
        }
    }

    /// <inheritdoc />
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task SendReminderForContributionAsync(Guid contributionId, string? triggeredBy = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting manual single contribution reminder job for Contribution ID: {ContributionId}. TriggeredBy: {TriggeredBy}", contributionId, triggeredBy ?? "Manual-API");

        try
        {
            var contribution = await _context.Contributions
                .Include(c => c.User)
                .Include(c => c.StatusItem)
                .Include(c => c.Event)
                    .ThenInclude(e => e!.EventType)
                .FirstOrDefaultAsync(c => c.ContributionId == contributionId && !c.IsDeleted, cancellationToken);

            if (contribution == null)
            {
                _logger.LogWarning("Contribution with ID {ContributionId} not found or deleted.", contributionId);
                return;
            }

            if (IsContributionPaid(contribution))
            {
                _logger.LogInformation("Contribution {ContributionId} is already Paid or Completed. Skipping reminder.", contributionId);
                return;
            }

            var user = contribution.User ?? await _context.Users.FirstOrDefaultAsync(u => u.UserId == contribution.UserId, cancellationToken);
            if (user == null || user.IsDeleted || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("Member for Contribution {ContributionId} (UserId: {UserId}) is invalid, inactive, or has no email address.", contributionId, contribution.UserId);
                return;
            }

            var systemSettings = await GetSystemSettingsSafeAsync(cancellationToken);
            var reminderInterval = GetReminderInterval(systemSettings);

            if (contribution.LastReminderSentAt.HasValue && contribution.LastReminderSentAt.Value.Add(reminderInterval) > DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Duplicate reminder prevented for Contribution {ContributionId}. Last reminder was sent at {LastSentAt} UTC (< {Interval} interval).",
                    contributionId, contribution.LastReminderSentAt.Value, reminderInterval);
                return;
            }

            var eventItem = contribution.Event;
            if (eventItem == null || eventItem.IsDeleted)
            {
                _logger.LogWarning("Associated event for Contribution {ContributionId} is missing or deleted.", contributionId);
                return;
            }
            await SendPersonalizedReminderEmailAsync(contribution, user, eventItem, systemSettings, cancellationToken);

            contribution.LastReminderSentAt = DateTime.UtcNow;
            contribution.ReminderCount += 1;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully sent reminder email for Contribution {ContributionId} to {RecipientEmail}. Total reminder count: {Count}",
                contributionId, user.Email, contribution.ReminderCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing manual reminder for Contribution ID: {ContributionId}", contributionId);
            throw;
        }
    }

    private async Task<(int Evaluated, int Sent, int SkippedPaid, int SkippedDuplicate, int Failed)> ProcessRemindersForEventInternalAsync(
        Event eventItem,
        SystemSettingsDto? systemSettings,
        string triggerSource,
        CancellationToken cancellationToken)
    {
        int evaluated = 0;
        int sent = 0;
        int skippedPaid = 0;
        int skippedDuplicate = 0;
        int failed = 0;

        var contributions = await _context.Contributions
            .Include(c => c.User)
            .Include(c => c.StatusItem)
            .Where(c => c.EventId == eventItem.EventId && !c.IsDeleted && c.IsActive && c.Amount > 0)
            .ToListAsync(cancellationToken);

        evaluated = contributions.Count;

        var reminderInterval = GetReminderInterval(systemSettings);
        var maxReminders = int.TryParse(systemSettings?.MaxReminders, out var mr) && mr > 0 ? mr : 3;

        var missingUserIds = contributions.Where(c => c.User == null).Select(c => c.UserId).Distinct().ToList();
        var fallbackUsers = new Dictionary<Guid, AppUser>();
        if (missingUserIds.Count > 0)
        {
            var loadedUsers = await _context.Users.Where(u => missingUserIds.Contains(u.UserId)).ToListAsync(cancellationToken);
            fallbackUsers = loadedUsers.ToDictionary(u => u.UserId);
        }

        foreach (var contribution in contributions)
        {
            // 1. Never send reminders for already Paid / Completed / Verified contributions
            if (IsContributionPaid(contribution))
            {
                skippedPaid++;
                continue;
            }

            var user = contribution.User ?? (fallbackUsers.TryGetValue(contribution.UserId, out var fu) ? fu : null);

            // 2. Validate member email
            if (user == null || user.IsDeleted || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("User for Contribution {ContributionId} (UserId: {UserId}) is inactive, deleted, or has empty email. Skipping.", contribution.ContributionId, contribution.UserId);
                continue;
            }

            // 3. Max reminders limit check
            if (contribution.ReminderCount >= maxReminders)
            {
                _logger.LogInformation(
                    "Skipping reminder for user {Email} on event {EventName}. Maximum reminder count ({Max}) reached.",
                    user.Email, eventItem.EventName, maxReminders);
                skippedDuplicate++;
                continue;
            }

            // 4. Prevent duplicate reminders within configured interval (Minutes, Hours, or Days)
            if (contribution.LastReminderSentAt.HasValue && contribution.LastReminderSentAt.Value.Add(reminderInterval) > DateTime.UtcNow)
            {
                _logger.LogInformation(
                    "Skipping duplicate reminder for user {Email} on event {EventName}. Last sent: {LastSentAt} UTC (< {Interval} interval).",
                    user.Email, eventItem.EventName, contribution.LastReminderSentAt.Value, reminderInterval);
                skippedDuplicate++;
                continue;
            }

            try
            {
                await SendPersonalizedReminderEmailAsync(contribution, user, eventItem, systemSettings, cancellationToken);

                contribution.LastReminderSentAt = DateTime.UtcNow;
                contribution.ReminderCount += 1;
                sent++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed to send reminder email to {RecipientEmail} for Contribution {ContributionId}", user.Email, contribution.ContributionId);
            }
        }

        if (sent > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (evaluated, sent, skippedPaid, skippedDuplicate, failed);
    }

    private async Task SendPersonalizedReminderEmailAsync(
        Contribution contribution,
        AppUser user,
        Event eventItem,
        SystemSettingsDto? systemSettings,
        CancellationToken cancellationToken)
    {
        var memberName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Username;
        var recipientEmail = user.Email;
        var eventName = eventItem.EventName;
        var eventTypeName = eventItem.EventType?.EventTypeName ?? "Event";
        var eventDate = eventItem.EventDate;
        var pendingAmount = contribution.Amount;

        var orgName = !string.IsNullOrWhiteSpace(systemSettings?.OrgName)
            ? systemSettings.OrgName
            : "Team Contribution Management";

        var upiReceiverName = !string.IsNullOrWhiteSpace(systemSettings?.QrReceiverName)
            ? systemSettings.QrReceiverName
            : CommonConstants.Defaults.DefaultPayeeName;

        var upiId = !string.IsNullOrWhiteSpace(systemSettings?.QrUpiId)
            ? systemSettings.QrUpiId
            : CommonConstants.Defaults.DefaultUpiId;

        var formattedAmount = pendingAmount % 1 == 0
            ? $"₹{pendingAmount:N0}"
            : $"₹{pendingAmount:F2}";

        var formattedDueDate = eventDate.ToString("dd MMMM yyyy");

        // Generate UPI payment deep link
        var upiPaymentUri = $"upi://pay?pa={Uri.EscapeDataString(upiId)}&pn={Uri.EscapeDataString(upiReceiverName)}&am={pendingAmount:F2}&cu=INR&tn={Uri.EscapeDataString("Contribution for " + eventName)}";

        // Generate dynamic high-resolution PNG QR Code using QRCoder
        string qrDataUri = string.Empty;
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(upiPaymentUri, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = pngQrCode.GetGraphic(20);
            string base64Qr = Convert.ToBase64String(qrBytes);
            qrDataUri = $"data:image/png;base64,{base64Qr}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate in-memory QRCoder PNG. Falling back to public QR API URL.");
            qrDataUri = $"https://api.qrserver.com/v1/create-qr-code/?size=260x260&margin=8&data={Uri.EscapeDataString(upiPaymentUri)}";
        }

        var frontendBaseUrl = _configuration["Cors:AllowedOrigins:0"] ?? CommonConstants.Defaults.DefaultFrontendUrl;
        var portalPaymentUrl = $"{frontendBaseUrl}/contributions";

        var (resolvedSubject, resolvedDescription) = ResolveEmailTemplate(
            systemSettings,
            eventTypeName,
            memberName,
            eventName,
            formattedAmount,
            formattedDueDate,
            orgName,
            upiPaymentUri,
            qrDataUri);

        var subject = !string.IsNullOrWhiteSpace(resolvedSubject)
            ? resolvedSubject
            : $"Payment Reminder: {eventName} - Contribution Due ({formattedAmount})";

        var body = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{subject}</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f1f5f9;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            color: #1e293b;
        }}
        .email-container {{
            max-width: 600px;
            margin: 30px auto;
            background: #ffffff;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
            border: 1px solid #e2e8f0;
        }}
        .email-header {{
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            padding: 36px 30px;
            text-align: center;
            color: #ffffff;
        }}
        .email-header .badge {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.2);
            color: #ffffff;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 1.5px;
            padding: 6px 14px;
            border-radius: 20px;
            text-transform: uppercase;
            margin-bottom: 12px;
            border: 1px solid rgba(255, 255, 255, 0.3);
        }}
        .email-header h1 {{
            margin: 0 0 6px 0;
            font-size: 24px;
            font-weight: 800;
            letter-spacing: -0.5px;
        }}
        .email-header p {{
            margin: 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .email-body {{
            padding: 32px 30px;
        }}
        .greeting {{
            font-size: 16px;
            font-weight: 600;
            color: #0f172a;
            margin-bottom: 16px;
        }}
        .message-intro {{
            font-size: 14px;
            line-height: 1.6;
            color: #475569;
            margin-bottom: 24px;
            white-space: pre-line;
        }}
        .details-card {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 24px;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px dashed #e2e8f0;
            font-size: 14px;
        }}
        .detail-row:last-child {{
            border-bottom: none;
            padding-bottom: 0;
        }}
        .detail-label {{
            color: #64748b;
            font-weight: 500;
        }}
        .detail-value {{
            color: #0f172a;
            font-weight: 700;
            text-align: right;
        }}
        .amount-banner {{
            background: #fdf4ff;
            border: 1.5px solid #d946ef;
            border-radius: 12px;
            padding: 16px;
            text-align: center;
            margin-bottom: 28px;
        }}
        .amount-banner .label {{
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 1px;
            font-weight: 700;
            color: #a21caf;
            margin-bottom: 4px;
        }}
        .amount-banner .value {{
            font-size: 28px;
            font-weight: 800;
            color: #86198f;
        }}
        .qr-section {{
            text-align: center;
            background: #faf5ff;
            border: 1px solid #e9d5ff;
            border-radius: 16px;
            padding: 24px;
            margin-bottom: 28px;
        }}
        .qr-wrapper {{
            display: inline-block;
            padding: 12px;
            background: #ffffff;
            border-radius: 12px;
            border: 2px solid #7c3aed;
            box-shadow: 0 4px 12px rgba(124, 58, 237, 0.12);
            margin-bottom: 12px;
        }}
        .qr-wrapper img {{
            display: block;
            width: 220px;
            height: 220px;
            border-radius: 6px;
        }}
        .upi-details {{
            font-size: 13px;
            color: #4b5563;
        }}
        .upi-details strong {{
            color: #312e81;
        }}
        .btn-pay {{
            display: block;
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 14px 28px;
            font-size: 16px;
            font-weight: 700;
            border-radius: 10px;
            text-align: center;
            margin: 20px 0;
            box-shadow: 0 4px 14px rgba(79, 70, 229, 0.35);
        }}
        .btn-portal {{
            display: block;
            background: #ffffff;
            color: #4f46e5 !important;
            border: 1.5px solid #4f46e5;
            text-decoration: none;
            padding: 11px 24px;
            font-size: 14px;
            font-weight: 600;
            border-radius: 10px;
            text-align: center;
            margin-bottom: 24px;
        }}
        .notice {{
            background: #fffbeb;
            border-left: 4px solid #f59e0b;
            padding: 12px 16px;
            border-radius: 6px;
            font-size: 12.5px;
            color: #92400e;
            line-height: 1.5;
            margin-bottom: 24px;
        }}
        .email-footer {{
            background: #f8fafc;
            border-top: 1px solid #e2e8f0;
            padding: 24px 30px;
            text-align: center;
            font-size: 12px;
            color: #94a3b8;
            line-height: 1.5;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <div class=""badge"">Contribution Reminder</div>
            <h1>{eventName}</h1>
            <p>Your team contribution is awaiting completion</p>
        </div>

        <div class=""email-body"">
            <div class=""message-intro"">
                {resolvedDescription}
            </div>

            <div class=""amount-banner"">
                <div class=""label"">Pending Amount Due</div>
                <div class=""value"">{formattedAmount}</div>
            </div>

            <div class=""details-card"">
                <table style=""width: 100%; border-collapse: collapse;"">
                    <tr style=""border-bottom: 1px dashed #e2e8f0;"">
                        <td style=""padding: 8px 0; color: #64748b; font-size: 14px;"">Member Name</td>
                        <td style=""padding: 8px 0; color: #0f172a; font-weight: 700; text-align: right; font-size: 14px;"">{memberName}</td>
                    </tr>
                    <tr style=""border-bottom: 1px dashed #e2e8f0;"">
                        <td style=""padding: 8px 0; color: #64748b; font-size: 14px;"">Event Name</td>
                        <td style=""padding: 8px 0; color: #0f172a; font-weight: 700; text-align: right; font-size: 14px;"">{eventName}</td>
                    </tr>
                    <tr style=""border-bottom: 1px dashed #e2e8f0;"">
                        <td style=""padding: 8px 0; color: #64748b; font-size: 14px;"">Event Date</td>
                        <td style=""padding: 8px 0; color: #0f172a; font-weight: 700; text-align: right; font-size: 14px;"">{formattedDueDate}</td>
                    </tr>
                    <tr>
                        <td style=""padding: 8px 0; color: #64748b; font-size: 14px;"">Payment Status</td>
                        <td style=""padding: 8px 0; color: #dc2626; font-weight: 700; text-align: right; font-size: 14px;"">&#x23F3; Pending</td>
                    </tr>
                </table>
            </div>

            <div class=""qr-section"">
                <div style=""font-size: 15px; font-weight: 700; color: #1e1b4b; margin-bottom: 14px;"">Scan QR Code via any UPI App</div>
                <div class=""qr-wrapper"">
                    <a href=""{upiPaymentUri}"" target=""_blank"" style=""display: block; text-decoration: none;"">
                        <img src=""{qrDataUri}"" alt=""UPI Payment QR Code - {upiReceiverName}"" />
                    </a>
                </div>
                <div class=""upi-details"">
                    <div>UPI ID: <strong>{upiId}</strong></div>
                    <div style=""margin-top: 3px; font-size: 12px; color: #6b7280;"">Receiver: <strong>{upiReceiverName}</strong></div>
                </div>
            </div>

            <a href=""{upiPaymentUri}"" class=""btn-pay"">&#x1F4B3; Pay {formattedAmount} via UPI App</a>
            <a href=""{portalPaymentUrl}"" class=""btn-portal"">View on Team Portal &amp; Submit Proof</a>

            <div class=""notice"">
                <strong>Important:</strong> If you have already made this payment via Cash or Bank Transfer, please submit your payment reference/UTR number on the portal or contact the organizer to verify your status.
            </div>

            <p style=""font-size: 14px; color: #475569; margin: 0;"">
                Warm regards,<br />
                <strong>{orgName}</strong>
            </p>
        </div>

        <div class=""email-footer"">
            This is an automated reminder email sent by {orgName}.<br />
            To manage your notification preferences or view payment history, please log in to your account.
        </div>
    </div>
</body>
</html>";

        await _emailService.SendEmailAsync(
            toEmail: recipientEmail,
            subject: subject,
            body: body,
            inlineImages: null,
            cancellationToken: cancellationToken);
    }

    private static (string Subject, string Description) ResolveEmailTemplate(
        SystemSettingsDto? systemSettings,
        string categoryName,
        string memberName,
        string eventName,
        string formattedAmount,
        string formattedDueDate,
        string orgName,
        string upiPaymentUri,
        string qrDataUri)
    {
        string rawSubject = string.Empty;
        string rawDescription = string.Empty;

        if (systemSettings?.CategoryTemplates != null)
        {
            try
            {
                var json = systemSettings.CategoryTemplates is string s
                    ? s
                    : System.Text.Json.JsonSerializer.Serialize(systemSettings.CategoryTemplates);

                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                var catKey = categoryName.ToLowerInvariant().Trim();

                System.Text.Json.JsonElement targetElem = default;
                if (root.TryGetProperty(catKey, out var elem) || root.TryGetProperty("all", out elem))
                {
                    targetElem = elem;
                }

                if (targetElem.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (targetElem.TryGetProperty("reminderSubject", out var rs))
                        rawSubject = rs.GetString() ?? string.Empty;
                    if (targetElem.TryGetProperty("reminderDescription", out var rd))
                        rawDescription = rd.GetString() ?? string.Empty;
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(rawSubject))
        {
            rawSubject = !string.IsNullOrWhiteSpace(systemSettings?.EmailSubject)
                ? systemSettings.EmailSubject
                : "Payment Reminder: Pending Contribution for {categoryName}";
        }

        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            rawDescription = !string.IsNullOrWhiteSpace(systemSettings?.EmailDescription)
                ? systemSettings.EmailDescription
                : "Dear {memberName},\n\nThis is a friendly reminder that your contribution of {amount} for {categoryName} is still pending.\nDue Date: {dueDate}\n\nPlease complete your payment at your earliest convenience using UPI:\n{paymentLink}\n\n{qrCode}\n\nThank you,\n{orgName}";
        }

        string resolvedSubject = rawSubject
            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
            .Replace("{categoryName}", categoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventName}", eventName, StringComparison.OrdinalIgnoreCase)
            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
            .Replace("{orgName}", orgName, StringComparison.OrdinalIgnoreCase);

        string resolvedDescription = rawDescription
            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
            .Replace("{categoryName}", categoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventName}", eventName, StringComparison.OrdinalIgnoreCase)
            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
            .Replace("{orgName}", orgName, StringComparison.OrdinalIgnoreCase)
            .Replace("{paymentLink}", upiPaymentUri, StringComparison.OrdinalIgnoreCase)
            .Replace("{qrCode}", string.Empty, StringComparison.OrdinalIgnoreCase);

        return (resolvedSubject, resolvedDescription);
    }

    private static bool IsContributionPaid(Contribution c)
    {
        if (c.PaymentDate.HasValue && (c.PaymentModeId.HasValue || c.CashAmount > 0 || c.UpiAmount > 0))
        {
            return true;
        }

        if (c.StatusItem != null)
        {
            var s = c.StatusItem.StatusName.ToLowerInvariant();
            if (s == "paid" || s == "verified" || s == "closed" || s == "completed" || s == "approved")
            {
                return true;
            }
        }

        return false;
    }

    private static TimeSpan GetReminderInterval(SystemSettingsDto? settings)
    {
        if (settings == null)
            return TimeSpan.FromDays(1);

        var unit = !string.IsNullOrWhiteSpace(settings.ReminderIntervalUnit) ? settings.ReminderIntervalUnit : "Days";
        var valStr = !string.IsNullOrWhiteSpace(settings.ReminderIntervalValue) ? settings.ReminderIntervalValue : settings.ReminderIntervalDays;

        if (double.TryParse(valStr, out var val) && val > 0)
        {
            return unit.ToLowerInvariant() switch
            {
                "minutes" or "minute" or "min" => TimeSpan.FromMinutes(val),
                "hours" or "hour" or "hr" => TimeSpan.FromHours(val),
                _ => TimeSpan.FromDays(val)
            };
        }

        return TimeSpan.FromDays(1);
    }

    private async Task<SystemSettingsDto?> GetSystemSettingsSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _settingService.GetSettingsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load system settings for reminder templates. Using defaults.");
            return null;
        }
    }
}
