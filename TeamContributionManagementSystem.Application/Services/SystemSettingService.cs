using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ILogger<SystemSettingService> _logger;
    private readonly ISystemSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SmtpSettings _smtpSettings;

    public SystemSettingService(
        ILogger<SystemSettingService> logger,
        ISystemSettingRepository settingRepository,
        IUnitOfWork unitOfWork,
        IOptions<SmtpSettings>? smtpOptions = null)
    {
        _logger = logger;
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
        _smtpSettings = smtpOptions?.Value ?? new SmtpSettings();
    }

    public async Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingRepository.GetAllAsync(cancellationToken);
            var map = settings.ToDictionary(x => x.SettingKey, x => x.SettingValue, StringComparer.OrdinalIgnoreCase);

            var dto = new SystemSettingsDto();

            if (map.TryGetValue(CommonConstants.SettingKeys.OrgName, out var orgName)) dto.OrgName = orgName;
            if (map.TryGetValue(CommonConstants.SettingKeys.BirthdayMembersExempt, out var birthdayMembersExempt)) dto.BirthdayMembersExempt = bool.TryParse(birthdayMembersExempt, out var bme) ? bme : false;
            if (map.TryGetValue(CommonConstants.SettingKeys.AllowedMultipleEvent, out var allowedMultipleEvent) ||
                map.TryGetValue("allowedMultipleEvent", out allowedMultipleEvent) ||
                map.TryGetValue("allowMultipleEvents", out allowedMultipleEvent) ||
                map.TryGetValue("allowed_multiple_event", out allowedMultipleEvent))
            {
                dto.AllowedMultipleEvent = bool.TryParse(allowedMultipleEvent, out var ame) ? ame : false;
            }
            if (map.TryGetValue(CommonConstants.SettingKeys.DefaultCurrency, out var defaultCurrency)) dto.DefaultCurrency = defaultCurrency;
            if (map.TryGetValue(CommonConstants.SettingKeys.TimeZone, out var timeZone)) dto.TimeZone = timeZone;

            // Email & SMTP configuration populated directly from backend secrets / SmtpSettings
            var secretFromEmail = !string.IsNullOrWhiteSpace(_smtpSettings.FromAddress)
                ? _smtpSettings.FromAddress
                : (!string.IsNullOrWhiteSpace(_smtpSettings.Username) ? _smtpSettings.Username : string.Empty);
            var secretFromName = !string.IsNullOrWhiteSpace(_smtpSettings.FromName)
                ? _smtpSettings.FromName
                : string.Empty;
            var secretHost = !string.IsNullOrWhiteSpace(_smtpSettings.Host)
                ? _smtpSettings.Host
                : string.Empty;
            var secretPort = _smtpSettings.Port > 0
                ? _smtpSettings.Port.ToString()
                : string.Empty;
            var secretEncryption = _smtpSettings.EnableSsl ? "TLS" : string.Empty;

            dto.FromEmail = !string.IsNullOrWhiteSpace(secretFromEmail) ? secretFromEmail : string.Empty;
            dto.FromName = !string.IsNullOrWhiteSpace(secretFromName) ? secretFromName : string.Empty;
            dto.SmtpHost = !string.IsNullOrWhiteSpace(secretHost) ? secretHost : string.Empty;
            dto.SmtpPort = !string.IsNullOrWhiteSpace(secretPort) ? secretPort : string.Empty;
            dto.Encryption = !string.IsNullOrWhiteSpace(secretEncryption) ? secretEncryption : string.Empty;

            if (string.IsNullOrWhiteSpace(dto.FromEmail) && map.TryGetValue(CommonConstants.SettingKeys.FromEmail, out var fromEmail) && !string.IsNullOrWhiteSpace(fromEmail) && !fromEmail.Contains("unit1a.com", StringComparison.OrdinalIgnoreCase))
            {
                dto.FromEmail = fromEmail;
            }
            if (string.IsNullOrWhiteSpace(dto.FromName) && map.TryGetValue(CommonConstants.SettingKeys.FromName, out var fromName) && !string.IsNullOrWhiteSpace(fromName) && !fromName.Contains("Unit 1A", StringComparison.OrdinalIgnoreCase))
            {
                dto.FromName = fromName;
            }
            if (string.IsNullOrWhiteSpace(dto.SmtpHost) && map.TryGetValue(CommonConstants.SettingKeys.SmtpHost, out var smtpHost) && !string.IsNullOrWhiteSpace(smtpHost))
            {
                dto.SmtpHost = smtpHost;
            }
            if (string.IsNullOrWhiteSpace(dto.SmtpPort) && map.TryGetValue(CommonConstants.SettingKeys.SmtpPort, out var smtpPort) && !string.IsNullOrWhiteSpace(smtpPort))
            {
                dto.SmtpPort = smtpPort;
            }
            if (string.IsNullOrWhiteSpace(dto.Encryption) && map.TryGetValue(CommonConstants.SettingKeys.Encryption, out var encryption) && !string.IsNullOrWhiteSpace(encryption))
            {
                dto.Encryption = encryption;
            }

            if (map.TryGetValue(CommonConstants.SettingKeys.OtpExpiry, out var otpExpiry)) dto.OtpExpiry = otpExpiry;
            if (map.TryGetValue(CommonConstants.SettingKeys.MaxRetry, out var maxRetry)) dto.MaxRetry = maxRetry;
            if (map.TryGetValue(CommonConstants.SettingKeys.EnableOtpLogin, out var enableOtpLogin)) dto.EnableOtpLogin = bool.TryParse(enableOtpLogin, out var b) ? b : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.Enable2faAdmin, out var enable2faAdmin)) dto.Enable2faAdmin = bool.TryParse(enable2faAdmin, out var b2) ? b2 : false;

            if (map.TryGetValue(CommonConstants.SettingKeys.EnableEmailNotif, out var enableEmailNotif)) dto.EnableEmailNotif = bool.TryParse(enableEmailNotif, out var ben) ? ben : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifNewMember, out var notifNewMember)) dto.NotifNewMember = bool.TryParse(notifNewMember, out var bnm) ? bnm : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifPaymentConfirm, out var notifPaymentConfirm)) dto.NotifPaymentConfirm = bool.TryParse(notifPaymentConfirm, out var bpc) ? bpc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifEventReminder, out var notifEventReminder)) dto.NotifEventReminder = bool.TryParse(notifEventReminder, out var ber) ? ber : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifSupportTicket, out var notifSupportTicket)) dto.NotifSupportTicket = bool.TryParse(notifSupportTicket, out var bst) ? bst : false;

            if (map.TryGetValue(CommonConstants.SettingKeys.EnableAuditLogs, out var enableAuditLogs)) dto.EnableAuditLogs = bool.TryParse(enableAuditLogs, out var bal) ? bal : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogUserLogin, out var logUserLogin)) dto.LogUserLogin = bool.TryParse(logUserLogin, out var lul) ? lul : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogDataChanges, out var logDataChanges)) dto.LogDataChanges = bool.TryParse(logDataChanges, out var ldc) ? ldc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogConfigChanges, out var logConfigChanges)) dto.LogConfigChanges = bool.TryParse(logConfigChanges, out var lcc) ? lcc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.RetentionPeriod, out var retentionPeriod)) dto.RetentionPeriod = retentionPeriod;

            if (map.TryGetValue(CommonConstants.SettingKeys.EmailSubject, out var emailSubject) && !string.IsNullOrWhiteSpace(emailSubject))
            {
                dto.EmailSubject = emailSubject;
            }
            else
            {
                dto.EmailSubject = "Contribution Notice - {categoryName}";
            }

            if (map.TryGetValue(CommonConstants.SettingKeys.EmailDescription, out var emailDescription) && !string.IsNullOrWhiteSpace(emailDescription))
            {
                dto.EmailDescription = CleanQrAndPaymentLinks(emailDescription);
            }
            else
            {
                dto.EmailDescription = "Dear {memberName},\n\nThis is a notification regarding your contribution for {categoryName} of {amount}, due by {dueDate}.\n\nPlease log in to the portal to view details and complete your contribution payment.\n\nThank you,\n{orgName}";
            }

            if (map.TryGetValue(CommonConstants.SettingKeys.SelectedTemplateCategoryId, out var selCatId)) dto.SelectedTemplateCategoryId = selCatId;
            if (map.TryGetValue(CommonConstants.SettingKeys.EnableMonthlyEmail, out var enableMonthlyEmail)) dto.EnableMonthlyEmail = bool.TryParse(enableMonthlyEmail, out var eme) ? eme : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.EnableReminderEmail, out var enableReminderEmail)) dto.EnableReminderEmail = bool.TryParse(enableReminderEmail, out var ere) ? ere : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.ReminderIntervalDays, out var remDays)) dto.ReminderIntervalDays = remDays;
            if (map.TryGetValue(CommonConstants.SettingKeys.ReminderIntervalValue, out var remVal)) dto.ReminderIntervalValue = remVal;
            else dto.ReminderIntervalValue = dto.ReminderIntervalDays;
            if (map.TryGetValue(CommonConstants.SettingKeys.ReminderIntervalUnit, out var remUnit)) dto.ReminderIntervalUnit = remUnit;
            if (map.TryGetValue(CommonConstants.SettingKeys.MaxReminders, out var maxRem)) dto.MaxReminders = maxRem;
            if (map.TryGetValue(CommonConstants.SettingKeys.CategoryTemplates, out var catTemplates) && !string.IsNullOrWhiteSpace(catTemplates))
            {
                var sanitizedCatTemplates = CleanQrAndPaymentLinks(catTemplates);
                try
                {
                    dto.CategoryTemplates = System.Text.Json.JsonSerializer.Deserialize<object>(sanitizedCatTemplates);
                }
                catch
                {
                    dto.CategoryTemplates = sanitizedCatTemplates;
                }
            }

            if (map.TryGetValue(CommonConstants.SettingKeys.PaymentQrConfigs, out var paymentQrConfigs) && !string.IsNullOrWhiteSpace(paymentQrConfigs))
            {
                try
                {
                    dto.PaymentQrConfigs = System.Text.Json.JsonSerializer.Deserialize<object>(paymentQrConfigs);
                }
                catch
                {
                    dto.PaymentQrConfigs = paymentQrConfigs;
                }
            }
            else
            {
                var qrList = await _settingRepository.GetAllEventTypePaymentSettingsAsync(cancellationToken);
                if (qrList != null && qrList.Count > 0)
                {
                    var qrMap = qrList.ToDictionary(
                        x => x.EventTypeName,
                        x => new
                        {
                            receiverName = x.ReceiverName,
                            upiId = x.UpiId,
                            qrMode = x.QrMode,
                            qrImage = x.QrImageUrl,
                            isConfigured = x.IsConfigured
                        },
                        StringComparer.OrdinalIgnoreCase);
                    dto.PaymentQrConfigs = qrMap;
                }
            }

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetSettingsAsync));
            throw;
        }
    }

    public async Task<SystemSettingsDto> UpdateSettingsAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            string catTemplatesJson = settings.CategoryTemplates switch
            {
                null => string.Empty,
                string s => s,
                System.Text.Json.JsonElement elem => elem.GetRawText(),
                _ => System.Text.Json.JsonSerializer.Serialize(settings.CategoryTemplates)
            };

            string paymentQrConfigsJson = settings.PaymentQrConfigs switch
            {
                null => string.Empty,
                string s => s,
                System.Text.Json.JsonElement elem => elem.GetRawText(),
                _ => System.Text.Json.JsonSerializer.Serialize(settings.PaymentQrConfigs)
            };

            var multipleEventsVal = settings.AllowedMultipleEvent.ToString().ToLowerInvariant();
            var dict = new Dictionary<string, (string Value, string Category)>
            {
                [CommonConstants.SettingKeys.OrgName] = (settings.OrgName ?? "Unit 1A Residents Association", CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.BirthdayMembersExempt] = (settings.BirthdayMembersExempt.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.AllowedMultipleEvent] = (multipleEventsVal, CommonConstants.SettingCategories.General),
                ["allowedMultipleEvent"] = (multipleEventsVal, CommonConstants.SettingCategories.General),
                ["allowMultipleEvents"] = (multipleEventsVal, CommonConstants.SettingCategories.General),
                ["allowed_multiple_event"] = (multipleEventsVal, CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.DefaultCurrency] = (settings.DefaultCurrency, CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.TimeZone] = (settings.TimeZone, CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.FromEmail] = (settings.FromEmail, CommonConstants.SettingCategories.Email),
                [CommonConstants.SettingKeys.FromName] = (settings.FromName, CommonConstants.SettingCategories.Email),
                [CommonConstants.SettingKeys.SmtpHost] = (settings.SmtpHost, CommonConstants.SettingCategories.Email),
                [CommonConstants.SettingKeys.SmtpPort] = (settings.SmtpPort, CommonConstants.SettingCategories.Email),
                [CommonConstants.SettingKeys.Encryption] = (settings.Encryption, CommonConstants.SettingCategories.Email),
                [CommonConstants.SettingKeys.OtpExpiry] = (settings.OtpExpiry, CommonConstants.SettingCategories.Security),
                [CommonConstants.SettingKeys.MaxRetry] = (settings.MaxRetry, CommonConstants.SettingCategories.Security),
                [CommonConstants.SettingKeys.EnableOtpLogin] = (settings.EnableOtpLogin.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Security),
                [CommonConstants.SettingKeys.Enable2faAdmin] = (settings.Enable2faAdmin.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Security),
                [CommonConstants.SettingKeys.EnableEmailNotif] = (settings.EnableEmailNotif.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Notifications),
                [CommonConstants.SettingKeys.NotifNewMember] = (settings.NotifNewMember.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Notifications),
                [CommonConstants.SettingKeys.NotifPaymentConfirm] = (settings.NotifPaymentConfirm.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Notifications),
                [CommonConstants.SettingKeys.NotifEventReminder] = (settings.NotifEventReminder.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Notifications),
                [CommonConstants.SettingKeys.NotifSupportTicket] = (settings.NotifSupportTicket.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Notifications),
                [CommonConstants.SettingKeys.EnableAuditLogs] = (settings.EnableAuditLogs.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogUserLogin] = (settings.LogUserLogin.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogDataChanges] = (settings.LogDataChanges.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogConfigChanges] = (settings.LogConfigChanges.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.RetentionPeriod] = (settings.RetentionPeriod, CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.EmailSubject] = (settings.EmailSubject ?? string.Empty, CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.EmailDescription] = (settings.EmailDescription ?? string.Empty, CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.CategoryTemplates] = (catTemplatesJson, CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.PaymentQrConfigs] = (paymentQrConfigsJson, CommonConstants.SettingCategories.PaymentQr),
                [CommonConstants.SettingKeys.SelectedTemplateCategoryId] = (settings.SelectedTemplateCategoryId ?? string.Empty, CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.EnableMonthlyEmail] = (settings.EnableMonthlyEmail.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.EnableReminderEmail] = (settings.EnableReminderEmail.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.ReminderIntervalDays] = (settings.ReminderIntervalDays ?? "10", CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.ReminderIntervalValue] = (settings.ReminderIntervalValue ?? settings.ReminderIntervalDays ?? "10", CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.ReminderIntervalUnit] = (settings.ReminderIntervalUnit ?? "Days", CommonConstants.SettingCategories.EmailTemplate),
                [CommonConstants.SettingKeys.MaxReminders] = (settings.MaxReminders ?? "3", CommonConstants.SettingCategories.EmailTemplate)
            };

            var existingList = await _settingRepository.GetAllAsync(cancellationToken);
            var existingMap = existingList.ToDictionary(x => x.SettingKey, StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<SystemSetting>();

            foreach (var kvp in dict)
            {
                var key = kvp.Key;
                var val = kvp.Value.Value;
                var cat = kvp.Value.Category;

                if (existingMap.TryGetValue(key, out var entity))
                {
                    entity.SettingValue = val;
                    entity.Category = cat;
                    entity.ModifiedBy = CommonMethods.ParseNullableGuid(user);
                    entity.ModifiedOn = DateTime.UtcNow;
                    _settingRepository.Update(entity);
                }
                else
                {
                    toAdd.Add(new SystemSetting
                    {
                        SettingId = Guid.NewGuid(),
                        SettingKey = key,
                        SettingValue = val,
                        Category = cat,
                        CreatedBy = CommonMethods.ParseNullableGuid(user),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                await _settingRepository.AddRangeAsync(toAdd, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(paymentQrConfigsJson))
            {
                await _settingRepository.SyncEventTypePaymentSettingsAsync(paymentQrConfigsJson, cancellationToken);
            }

            EnsureGpayImageExists();
            _logger.LogInformation(CommonLogMessages.Settings.SettingsUpdated, user ?? string.Empty);

            return await GetSettingsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateSettingsAsync));
            throw;
        }
    }

    public async Task<SystemSettingsDto> ResetSettingsAsync(string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var defaults = new SystemSettingsDto
            {
                FromEmail = !string.IsNullOrWhiteSpace(_smtpSettings.FromAddress) ? _smtpSettings.FromAddress : _smtpSettings.Username,
                FromName = !string.IsNullOrWhiteSpace(_smtpSettings.FromName) ? _smtpSettings.FromName : string.Empty,
                SmtpHost = _smtpSettings.Host,
                SmtpPort = _smtpSettings.Port > 0 ? _smtpSettings.Port.ToString() : string.Empty,
                Encryption = _smtpSettings.EnableSsl ? "TLS" : string.Empty
            };
            var result = await UpdateSettingsAsync(defaults, user, cancellationToken);
            _logger.LogInformation(CommonLogMessages.Settings.SettingsReset, user ?? string.Empty);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(ResetSettingsAsync));
            throw;
        }
    }

    private static void EnsureGpayImageExists()
    {
        try
        {
            var targetDir = Path.Combine(AppContext.BaseDirectory, CommonConstants.Defaults.WwwRoot);
            var targetFile = Path.Combine(targetDir, CommonConstants.Defaults.GpayFileName);
            if (!File.Exists(targetFile))
            {
                var candidateSources = new[]
                {
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.GpayFileName)),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.API", CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.GpayFileName)),
                    Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.GpayFileName)
                };
                var source = candidateSources.FirstOrDefault(File.Exists);
                if (source != null)
                {
                    Directory.CreateDirectory(targetDir);
                    File.Copy(source, targetFile, true);
                }
            }
        }
        catch { }
    }

    private static string CleanQrAndPaymentLinks(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        const string replacementText = "Please log in to the portal to view details and complete your contribution payment.";

        var phrasesToClean = new[]
        {
            "Please scan the attached dynamic UPI QR code or click the payment link to pay:",
            "Please scan the dynamic UPI QR code below or tap the payment link to contribute:",
            "Please use the payment link or scan the dynamic UPI QR code:",
            "Pay via UPI link or scan the QR code:",
            "Please scan the QR code below or use the payment link:",
            "Please scan the dynamic UPI QR code or use the payment link:",
            "Please complete your payment at your earliest convenience using UPI:",
            "Please settle this via UPI so we can finalize bookings:",
            "Please scan the attached dynamic UPI QR code.",
            "Please scan the dynamic UPI QR code.",
            "Please scan the attached dynamic UPI QR code",
            "Please scan the dynamic UPI QR code"
        };

        var cleaned = text;

        foreach (var phrase in phrasesToClean)
        {
            cleaned = cleaned.Replace(phrase, replacementText, StringComparison.OrdinalIgnoreCase);
        }

        cleaned = cleaned
            .Replace("{paymentLink}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{qrCode}", string.Empty, StringComparison.OrdinalIgnoreCase);

        // Normalize 3+ escaped or raw newlines to double newlines
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\\n){3,}", @"\n\n");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"(\r?\n){3,}", "\n\n");

        return cleaned.Trim();
    }
}
