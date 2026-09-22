using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly Microsoft.Extensions.Logging.ILogger<SystemSettingService> _logger;
    private readonly ISystemSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SystemSettingService(Microsoft.Extensions.Logging.ILogger<SystemSettingService> logger, ISystemSettingRepository settingRepository, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingRepository.GetAllAsync(cancellationToken);
        var map = settings.ToDictionary(x => x.SettingKey, x => x.SettingValue, StringComparer.OrdinalIgnoreCase);

        var dto = new SystemSettingsDto();

        if (map.TryGetValue("orgName", out var orgName)) dto.OrgName = orgName;
        if (map.TryGetValue("defaultCurrency", out var defaultCurrency)) dto.DefaultCurrency = defaultCurrency;
        if (map.TryGetValue("timeZone", out var timeZone)) dto.TimeZone = timeZone;

        if (map.TryGetValue("fromEmail", out var fromEmail)) dto.FromEmail = fromEmail;
        if (map.TryGetValue("fromName", out var fromName)) dto.FromName = fromName;
        if (map.TryGetValue("smtpHost", out var smtpHost)) dto.SmtpHost = smtpHost;
        if (map.TryGetValue("smtpPort", out var smtpPort)) dto.SmtpPort = smtpPort;
        if (map.TryGetValue("encryption", out var encryption)) dto.Encryption = encryption;

        if (map.TryGetValue("otpExpiry", out var otpExpiry)) dto.OtpExpiry = otpExpiry;
        if (map.TryGetValue("maxRetry", out var maxRetry)) dto.MaxRetry = maxRetry;
        if (map.TryGetValue("enableOtpLogin", out var enableOtpLogin)) dto.EnableOtpLogin = bool.TryParse(enableOtpLogin, out var b) ? b : true;
        if (map.TryGetValue("enable2faAdmin", out var enable2faAdmin)) dto.Enable2faAdmin = bool.TryParse(enable2faAdmin, out var b) ? b : false;

        if (map.TryGetValue("enableEmailNotif", out var enableEmailNotif)) dto.EnableEmailNotif = bool.TryParse(enableEmailNotif, out var b) ? b : true;
        if (map.TryGetValue("notifNewMember", out var notifNewMember)) dto.NotifNewMember = bool.TryParse(notifNewMember, out var b) ? b : true;
        if (map.TryGetValue("notifPaymentConfirm", out var notifPaymentConfirm)) dto.NotifPaymentConfirm = bool.TryParse(notifPaymentConfirm, out var b) ? b : true;
        if (map.TryGetValue("notifEventReminder", out var notifEventReminder)) dto.NotifEventReminder = bool.TryParse(notifEventReminder, out var b) ? b : true;
        if (map.TryGetValue("notifSupportTicket", out var notifSupportTicket)) dto.NotifSupportTicket = bool.TryParse(notifSupportTicket, out var b) ? b : false;

        if (map.TryGetValue("qrReceiverName", out var qrReceiverName)) dto.QrReceiverName = qrReceiverName;
        if (map.TryGetValue("qrUpiId", out var qrUpiId)) dto.QrUpiId = qrUpiId;
        if (map.TryGetValue("qrImage", out var qrImage)) dto.QrImage = qrImage;

        if (map.TryGetValue("enableAuditLogs", out var enableAuditLogs)) dto.EnableAuditLogs = bool.TryParse(enableAuditLogs, out var b) ? b : true;
        if (map.TryGetValue("logUserLogin", out var logUserLogin)) dto.LogUserLogin = bool.TryParse(logUserLogin, out var b) ? b : true;
        if (map.TryGetValue("logDataChanges", out var logDataChanges)) dto.LogDataChanges = bool.TryParse(logDataChanges, out var b) ? b : true;
        if (map.TryGetValue("logConfigChanges", out var logConfigChanges)) dto.LogConfigChanges = bool.TryParse(logConfigChanges, out var b) ? b : true;
        if (map.TryGetValue("retentionPeriod", out var retentionPeriod)) dto.RetentionPeriod = retentionPeriod;

        return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSettingsAsync");
            throw;
        }
    }

    public async Task<SystemSettingsDto> UpdateSettingsAsync(SystemSettingsDto settings, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dict = new Dictionary<string, (string Value, string Category)>
        {
            ["orgName"] = (settings.OrgName, "General"),
            ["defaultCurrency"] = (settings.DefaultCurrency, "General"),
            ["timeZone"] = (settings.TimeZone, "General"),
            ["fromEmail"] = (settings.FromEmail, "Email"),
            ["fromName"] = (settings.FromName, "Email"),
            ["smtpHost"] = (settings.SmtpHost, "Email"),
            ["smtpPort"] = (settings.SmtpPort, "Email"),
            ["encryption"] = (settings.Encryption, "Email"),
            ["otpExpiry"] = (settings.OtpExpiry, "Security"),
            ["maxRetry"] = (settings.MaxRetry, "Security"),
            ["enableOtpLogin"] = (settings.EnableOtpLogin.ToString().ToLowerInvariant(), "Security"),
            ["enable2faAdmin"] = (settings.Enable2faAdmin.ToString().ToLowerInvariant(), "Security"),
            ["enableEmailNotif"] = (settings.EnableEmailNotif.ToString().ToLowerInvariant(), "Notifications"),
            ["notifNewMember"] = (settings.NotifNewMember.ToString().ToLowerInvariant(), "Notifications"),
            ["notifPaymentConfirm"] = (settings.NotifPaymentConfirm.ToString().ToLowerInvariant(), "Notifications"),
            ["notifEventReminder"] = (settings.NotifEventReminder.ToString().ToLowerInvariant(), "Notifications"),
            ["notifSupportTicket"] = (settings.NotifSupportTicket.ToString().ToLowerInvariant(), "Notifications"),
            ["qrReceiverName"] = (settings.QrReceiverName, "PaymentQr"),
            ["qrUpiId"] = (settings.QrUpiId, "PaymentQr"),
            ["qrImage"] = (settings.QrImage, "PaymentQr"),
            ["enableAuditLogs"] = (settings.EnableAuditLogs.ToString().ToLowerInvariant(), "Audit"),
            ["logUserLogin"] = (settings.LogUserLogin.ToString().ToLowerInvariant(), "Audit"),
            ["logDataChanges"] = (settings.LogDataChanges.ToString().ToLowerInvariant(), "Audit"),
            ["logConfigChanges"] = (settings.LogConfigChanges.ToString().ToLowerInvariant(), "Audit"),
            ["retentionPeriod"] = (settings.RetentionPeriod, "Audit")
        };

        var existingList = await _settingRepository.GetAllAsync(cancellationToken);
        var existingMap = existingList.ToDictionary(x => x.SettingKey, StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<SystemSetting>();

        foreach (var (key, (val, cat)) in dict)
        {
            if (existingMap.TryGetValue(key, out var entity))
            {
                entity.SettingValue = val;
                entity.Category = cat;
                entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
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
                    CreatedBy = string.IsNullOrWhiteSpace(user) ? "Admin" : user,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await _settingRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetSettingsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateSettingsAsync");
            throw;
        }
    }

    public async Task<SystemSettingsDto> ResetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var defaults = new SystemSettingsDto();
        return await UpdateSettingsAsync(defaults, "System", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetSettingsAsync");
            throw;
        }
    }
}
