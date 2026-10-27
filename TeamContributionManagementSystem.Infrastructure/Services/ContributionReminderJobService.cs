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
        var memberName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Username ?? "Member");
        var recipientEmail = user.Email ?? string.Empty;
        var eventName = eventItem.EventName ?? "Event";
        var eventTypeName = eventItem.EventType?.EventTypeName ?? "Event";
        var eventDate = eventItem.EventDate;
        var pendingAmount = contribution.Amount;

        // Get the event's actual description and strip internal metadata
        var eventDescription = System.Text.RegularExpressions.Regex
            .Replace(eventItem.Description ?? string.Empty, @"<!--contrib:[\s\S]*?-->", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Trim();

        var orgName = !string.IsNullOrWhiteSpace(systemSettings?.OrgName)
            ? systemSettings.OrgName
            : "Team Contribution Management";

        var formattedAmount = pendingAmount % 1 == 0
            ? $"₹{pendingAmount:N0}"
            : $"₹{pendingAmount:F2}";

        var formattedDueDate = eventDate.ToString("dd MMMM yyyy");

        var frontendBaseUrl = _configuration["Cors:AllowedOrigins:0"] ?? CommonConstants.Defaults.DefaultFrontendUrl;
        var portalPaymentUrl = $"{frontendBaseUrl}/contributions";

        // Query birthday celebrants and format celebrants list
        bool isBirthdayEvent = (eventTypeName ?? string.Empty).IndexOf("birthday", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               (eventName ?? string.Empty).IndexOf("birthday", StringComparison.OrdinalIgnoreCase) >= 0;

        List<AppUser> targetCelebrants = new();
        if (isBirthdayEvent)
        {
            try
            {
                var exemptCelebrantIds = await _context.Contributions
                    .Where(c => c.EventId == eventItem.EventId && c.Amount == 0)
                    .Select(c => c.MemberId)
                    .ToListAsync(cancellationToken);

                if (exemptCelebrantIds.Count > 0)
                {
                    targetCelebrants = await _context.Users
                        .Where(u => u.IsActive && !u.IsExited && exemptCelebrantIds.Contains(u.UserId))
                        .ToListAsync(cancellationToken);
                }

                if (targetCelebrants.Count == 0)
                {
                    targetCelebrants = await _context.Users
                        .Where(u => u.IsActive && !u.IsExited && u.DateOfBirth.Month == eventDate.Month)
                        .ToListAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve active birthday celebrants for reminder email");
            }
        }

        targetCelebrants = targetCelebrants.OrderBy(c => c.DateOfBirth.Day).ToList();

        Func<AppUser, DateTime> getCelebrantBirthDate = (m) =>
        {
            int birthMonth = m.DateOfBirth.Month >= 1 && m.DateOfBirth.Month <= 12 ? m.DateOfBirth.Month : (eventDate.Month >= 1 && eventDate.Month <= 12 ? eventDate.Month : DateTime.UtcNow.Month);
            int maxDays = DateTime.DaysInMonth(eventDate.Year, birthMonth);
            int birthDay = m.DateOfBirth.Day >= 1 && m.DateOfBirth.Day <= maxDays ? m.DateOfBirth.Day : Math.Min(Math.Max(1, m.DateOfBirth.Day), maxDays);
            return new DateTime(eventDate.Year, birthMonth, birthDay);
        };

        string celebrantsAndDatesHtml = string.Empty;
        if (isBirthdayEvent)
        {
            if (targetCelebrants.Count == 1)
            {
                var c = targetCelebrants[0];
                var bdayDate = getCelebrantBirthDate(c);
                celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"">Birthday Celebrant:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #7c3aed;"">{c.FullName}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Particular Birthday Date:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #c026d3;"">{bdayDate:MMMM dd, yyyy}</span>
                </div>";
            }
            else if (targetCelebrants.Count > 1)
            {
                var tableRows = string.Join("", targetCelebrants.Select(c =>
                {
                    var bdayDate = getCelebrantBirthDate(c);
                    return $@"
                        <tr style=""border-bottom: 1px dashed rgba(74, 63, 107, 0.1);"">
                            <td style=""padding: 8px 12px; color: #4a3f6b; font-weight: 700; font-size: 13.5px;"">&#x1F382; {c.FullName}</td>
                            <td align=""right"" style=""padding: 8px 12px; color: #c026d3; font-weight: 700; font-size: 13.5px;"">{bdayDate:MMMM dd, yyyy}</td>
                        </tr>";
                }));

                celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"" style=""vertical-align: top; padding-top: 4px;"">Birthday Celebrants &amp; Dates:</span>
                    <div class=""detail-value"" style=""display: block; margin-top: 6px;"">
                        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""border-collapse: collapse; background-color: #fbfaff; border-radius: 8px; border: 1.5px solid #ece8f8;"">
                            {tableRows}
                        </table>
                    </div>
                </div>";
            }
        }

        string eventDateLabel = isBirthdayEvent && !string.IsNullOrWhiteSpace(eventItem.EventDates) && eventItem.EventDates.Contains(",")
            ? "Event Dates:"
            : "Event Date:";

        string eventDateValueDisplay = FormatEventDatesForDisplay(eventItem.EventDates, eventDate, isBirthdayEvent, targetCelebrants);

        string totalAmountDisplay = isBirthdayEvent && targetCelebrants.Count > 1
            ? $"Rs.{eventItem.BaseAmount:F2} ({targetCelebrants.Count} celebrants combined)"
            : $"Rs.{eventItem.BaseAmount:F2}";


        // 1. Resolve Payment QR Setting for this Event Type
        EventTypePaymentSetting? paymentSetting = null;
        try
        {
            if (eventItem.EventTypeId != Guid.Empty)
            {
                paymentSetting = await _context.EventTypePaymentSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.EventTypeId == eventItem.EventTypeId, cancellationToken);
            }

            if (paymentSetting == null && !string.IsNullOrWhiteSpace(eventTypeName))
            {
                paymentSetting = await _context.EventTypePaymentSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.EventTypeName.ToLower() == eventTypeName.ToLower(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query EventTypePaymentSettings for event {EventName}", eventName);
        }

        if (paymentSetting == null && systemSettings?.PaymentQrConfigs != null)
        {
            try
            {
                var json = systemSettings.PaymentQrConfigs is string s
                    ? s
                    : System.Text.Json.JsonSerializer.Serialize(systemSettings.PaymentQrConfigs);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                var key = eventTypeName.Trim().ToLowerInvariant();
                if (root.TryGetProperty(key, out var elem) || root.TryGetProperty("all", out elem))
                {
                    var upi = elem.TryGetProperty("upiId", out var u) ? u.GetString() : null;
                    var rec = elem.TryGetProperty("receiverName", out var r) ? r.GetString() : null;
                    var mode = elem.TryGetProperty("qrMode", out var m) ? m.GetString() : "generated";
                    var img = elem.TryGetProperty("qrImage", out var qi) ? qi.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(upi) || !string.IsNullOrWhiteSpace(img))
                    {
                        paymentSetting = new EventTypePaymentSetting
                        {
                            EventTypeName = eventTypeName,
                            UpiId = upi ?? string.Empty,
                            ReceiverName = rec ?? orgName,
                            QrMode = mode ?? "generated",
                            QrImageUrl = img,
                            IsConfigured = true
                        };
                    }
                }
            }
            catch { }
        }

        var effectiveUpiId = !string.IsNullOrWhiteSpace(paymentSetting?.UpiId)
            ? paymentSetting.UpiId
            : string.Empty;
        var effectiveReceiverName = !string.IsNullOrWhiteSpace(paymentSetting?.ReceiverName)
            ? paymentSetting.ReceiverName
            : orgName;
        var isUploadedMode = paymentSetting?.QrMode?.Equals("uploaded", StringComparison.OrdinalIgnoreCase) == true && !string.IsNullOrWhiteSpace(paymentSetting?.QrImageUrl);

        var isQrConfigured = !string.IsNullOrWhiteSpace(effectiveUpiId) || isUploadedMode;

        var qrCodeCardHtml = string.Empty;
        InlineEmailImage? inlineImage = null;

        if (isQrConfigured)
        {
            var upiPaymentUri = $"upi://pay?pa={Uri.EscapeDataString(effectiveUpiId)}&pn={Uri.EscapeDataString(effectiveReceiverName)}&am={pendingAmount:F2}&cu=INR&tn={Uri.EscapeDataString($"Contribution for {eventName}")}";

            // 2. Generate or Load QR Code Image as Inline Attachment
            var qrContentId = $"qr_{Guid.NewGuid():N}";
            var tempDir = Path.Combine(Path.GetTempPath(), "ContributionManagement_EmailQr");
            Directory.CreateDirectory(tempDir);

            if (isUploadedMode && !string.IsNullOrWhiteSpace(paymentSetting?.QrImageUrl))
            {
                var rawImg = paymentSetting.QrImageUrl.Trim();
                if (rawImg.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var commaIdx = rawImg.IndexOf(',');
                        var base64 = commaIdx >= 0 ? rawImg.Substring(commaIdx + 1) : rawImg;
                        var imgBytes = Convert.FromBase64String(base64);
                        var ext = rawImg.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || rawImg.Contains("jpg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                        var mediaType = ext == "jpg" ? "image/jpeg" : "image/png";
                        var tempFilePath = Path.Combine(tempDir, $"{qrContentId}.{ext}");
                        await File.WriteAllBytesAsync(tempFilePath, imgBytes, cancellationToken);
                        inlineImage = new InlineEmailImage(qrContentId, tempFilePath, mediaType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decode uploaded QR base64 image; falling back to dynamic generation.");
                    }
                }
                else
                {
                    var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rawImg.TrimStart('/', '\\'));
                    if (File.Exists(localPath))
                    {
                        inlineImage = new InlineEmailImage(qrContentId, localPath, "image/png");
                    }
                }
            }

            if (inlineImage == null && !string.IsNullOrWhiteSpace(effectiveUpiId))
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(upiPaymentUri, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrBytes = qrCode.GetGraphic(20);
                var tempFilePath = Path.Combine(tempDir, $"{qrContentId}.png");
                await File.WriteAllBytesAsync(tempFilePath, qrBytes, cancellationToken);
                inlineImage = new InlineEmailImage(qrContentId, tempFilePath, "image/png");
            }

            if (inlineImage != null)
            {
                qrCodeCardHtml = $@"
            <div class=""qr-section"">
                <div style=""font-size: 13.5px; font-weight: 700; color: #4b5563; margin-bottom: 12px; text-transform: uppercase; letter-spacing: 0.5px;"">
                    Scan QR to Pay via Any UPI App (GPay / PhonePe / Paytm)
                </div>
                <div class=""qr-wrapper"">
                    <img src=""cid:{qrContentId}"" alt=""UPI Payment QR Code"" />
                </div>
                <div class=""upi-details"">
                    <div><strong>Payee Name:</strong> {effectiveReceiverName}</div>
                    <div style=""margin-top: 4px;""><strong>UPI ID:</strong> {effectiveUpiId}</div>
                    <div style=""margin-top: 6px; font-size: 12px; color: #7c3aed; font-weight: 600;"">
                        Amount is pre-filled automatically when scanned ({formattedAmount})
                    </div>
                </div>
                <div style=""text-align: center; margin-top: 14px;"">
                    <a href=""{upiPaymentUri}"" class=""btn-pay"" style=""display: inline-block; margin: 0; padding: 10px 24px; font-size: 14px;"">
                        Pay {formattedAmount} via UPI
                    </a>
                </div>
            </div>";
            }
        }

        var (resolvedSubject, resolvedDescription, qrEmbeddedInDesc) = ResolveEmailTemplate(
            systemSettings,
            eventTypeName ?? "Event",
            memberName ?? "Member",
            eventName ?? "Event",
            eventDescription ?? string.Empty,
            formattedAmount,
            formattedDueDate,
            orgName,
            portalPaymentUrl,
            qrCodeCardHtml);

        var qrSectionBlock = qrEmbeddedInDesc ? string.Empty : qrCodeCardHtml;

        var subject = !string.IsNullOrWhiteSpace(resolvedSubject)
            ? resolvedSubject
            : $"Payment Reminder: {eventName ?? "Event"} - Contribution Due ({formattedAmount})";

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
            background-color: #faf9fd;
            border: 1px solid rgba(74, 63, 107, 0.12);
            border-radius: 10px;
            padding: 24px;
            margin-bottom: 25px;
        }}
        .detail-row {{
            margin-bottom: 16px;
            border-bottom: 1px dashed rgba(74, 63, 107, 0.1);
            padding-bottom: 16px;
        }}
        .detail-row:last-child {{
            margin-bottom: 0;
            border-bottom: none;
            padding-bottom: 0;
        }}
        .detail-label {{
            font-weight: 700;
            color: #5b5280;
            display: inline-block;
            width: 160px;
        }}
        .detail-value {{
            color: #1e1a2e;
            display: inline-block;
        }}
        .amount-highlight {{
            font-size: 18px;
            color: #2d2550;
            font-weight: 800;
            background-color: #f0ecf9;
            padding: 4px 10px;
            border-radius: 6px;
            display: inline-block;
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
            margin: 0 auto;
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

            <div class=""details-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event Name:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{eventName}</span>
                </div>
                {celebrantsAndDatesHtml}
                <div class=""detail-row"">
                    <span class=""detail-label"">{eventDateLabel}</span>
                    <span class=""detail-value"" style=""font-weight: 600;"">{eventDateValueDisplay}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Total Amount:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{totalAmountDisplay}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Description:</span>
                    <span class=""detail-value"">{eventDescription}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Contribution Amount:</span>
                    <span class=""detail-value amount-highlight"">{formattedAmount}</span>
                </div>
            </div>

            {qrSectionBlock}

            <div style=""text-align: center; margin: 28px 0 20px 0;"">
                <a href=""{portalPaymentUrl}"" target=""_blank"" style=""display: inline-block; background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); color: #ffffff !important; text-decoration: none; padding: 14px 34px; font-size: 15px; font-weight: 700; border-radius: 10px; box-shadow: 0 4px 14px rgba(79, 70, 229, 0.35); letter-spacing: 0.3px;"">
                    Go to Contribution Page &amp; Pay
                </a>
            </div>

            <div class=""notice"">
                <strong>Important:</strong> If you have already made this payment via Cash or Bank Transfer, please contact the organizer or verify your payment status on the portal.
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
            inlineImages: inlineImage != null ? new[] { inlineImage } : null,
            cancellationToken: cancellationToken);
    }

    private static (string Subject, string Description, bool QrEmbeddedInDesc) ResolveEmailTemplate(
        SystemSettingsDto? systemSettings,
        string categoryName,
        string memberName,
        string eventName,
        string eventDescription,
        string formattedAmount,
        string formattedDueDate,
        string orgName,
        string portalContributionUrl,
        string qrCodeCardHtml = "")
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
                : "Dear {memberName},\n\nThis is a friendly reminder that your contribution of {amount} for {categoryName} is pending.\nDue Date: {dueDate}\n\n{eventDescription}\n\nPlease scan the dynamic UPI QR code below or log in to the portal to complete your contribution payment.\n\nThank you,\n{orgName}";
        }

        string resolvedSubject = rawSubject
            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
            .Replace("{categoryName}", categoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventName}", eventName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventDescription}", eventDescription, StringComparison.OrdinalIgnoreCase)
            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
            .Replace("{orgName}", orgName, StringComparison.OrdinalIgnoreCase);

        bool qrEmbeddedInDesc = false;
        string descriptionWithQr = rawDescription;
        if (!string.IsNullOrWhiteSpace(qrCodeCardHtml) && descriptionWithQr.Contains("{qrCode}", StringComparison.OrdinalIgnoreCase))
        {
            descriptionWithQr = descriptionWithQr.Replace("{qrCode}", qrCodeCardHtml, StringComparison.OrdinalIgnoreCase);
            qrEmbeddedInDesc = true;
        }
        else
        {
            descriptionWithQr = descriptionWithQr.Replace("{qrCode}", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        string resolvedDescription = descriptionWithQr
            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
            .Replace("{categoryName}", categoryName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventName}", eventName, StringComparison.OrdinalIgnoreCase)
            .Replace("{eventDescription}", eventDescription, StringComparison.OrdinalIgnoreCase)
            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
            .Replace("{orgName}", orgName, StringComparison.OrdinalIgnoreCase)
            .Replace("{paymentLink}", portalContributionUrl, StringComparison.OrdinalIgnoreCase);

        return (resolvedSubject, resolvedDescription, qrEmbeddedInDesc);
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

    private static string FormatEventDatesForDisplay(
        string? rawEventDates,
        DateTime fallbackDate,
        bool isBirthdayEvent = false,
        List<TeamContributionManagementSystem.Domain.Entities.User>? celebrants = null)
    {
        if (!string.IsNullOrWhiteSpace(rawEventDates))
        {
            var trimmed = rawEventDates.Trim();
            if (trimmed.StartsWith("{"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                    var formattedDates = new List<string>();

                    void ExtractDates(System.Text.Json.JsonElement element)
                    {
                        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var prop in element.EnumerateObject())
                            {
                                ExtractDates(prop.Value);
                            }
                        }
                        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                ExtractDates(item);
                            }
                        }
                        else if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var strVal = element.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(strVal))
                            {
                                if (DateTime.TryParse(strVal, out var parsedDt))
                                {
                                    formattedDates.Add(parsedDt.ToString("MMMM dd, yyyy"));
                                }
                                else
                                {
                                    formattedDates.Add(strVal);
                                }
                            }
                        }
                    }

                    ExtractDates(doc.RootElement);

                    var distinct = formattedDates.Distinct().ToList();
                    if (distinct.Count > 0)
                    {
                        return string.Join(", ", distinct);
                    }
                }
                catch { }
            }
            else
            {
                var parts = trimmed.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => DateTime.TryParse(p, out var dt) ? dt.ToString("MMMM dd, yyyy") : p)
                    .Distinct()
                    .ToList();

                if (parts.Count > 0)
                {
                    return string.Join(", ", parts);
                }
            }
        }

        if (isBirthdayEvent && celebrants != null && celebrants.Count > 0)
        {
            var bdayDates = celebrants
                .OrderBy(c => c.DateOfBirth.Day)
                .Select(c => $"{c.DateOfBirth.Day} {c.DateOfBirth:MMM}")
                .Distinct()
                .ToList();

            if (bdayDates.Count > 0)
            {
                return string.Join(", ", bdayDates);
            }
        }

        return fallbackDate.ToString("MMMM dd, yyyy");
    }
}
