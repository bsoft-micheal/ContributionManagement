using Microsoft.Extensions.Logging;
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

    public SystemSettingService(ILogger<SystemSettingService> logger, ISystemSettingRepository settingRepository, IUnitOfWork unitOfWork)
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

            if (map.TryGetValue(CommonConstants.SettingKeys.OrgName, out var orgName)) dto.OrgName = orgName;
            if (map.TryGetValue(CommonConstants.SettingKeys.BirthdayMembersExempt, out var birthdayMembersExempt)) dto.BirthdayMembersExempt = bool.TryParse(birthdayMembersExempt, out var bme) ? bme : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.DefaultCurrency, out var defaultCurrency)) dto.DefaultCurrency = defaultCurrency;
            if (map.TryGetValue(CommonConstants.SettingKeys.TimeZone, out var timeZone)) dto.TimeZone = timeZone;

            if (map.TryGetValue(CommonConstants.SettingKeys.FromEmail, out var fromEmail)) dto.FromEmail = fromEmail;
            if (map.TryGetValue(CommonConstants.SettingKeys.FromName, out var fromName)) dto.FromName = fromName;
            if (map.TryGetValue(CommonConstants.SettingKeys.SmtpHost, out var smtpHost)) dto.SmtpHost = smtpHost;
            if (map.TryGetValue(CommonConstants.SettingKeys.SmtpPort, out var smtpPort)) dto.SmtpPort = smtpPort;
            if (map.TryGetValue(CommonConstants.SettingKeys.Encryption, out var encryption)) dto.Encryption = encryption;

            if (map.TryGetValue(CommonConstants.SettingKeys.OtpExpiry, out var otpExpiry)) dto.OtpExpiry = otpExpiry;
            if (map.TryGetValue(CommonConstants.SettingKeys.MaxRetry, out var maxRetry)) dto.MaxRetry = maxRetry;
            if (map.TryGetValue(CommonConstants.SettingKeys.EnableOtpLogin, out var enableOtpLogin)) dto.EnableOtpLogin = bool.TryParse(enableOtpLogin, out var b) ? b : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.Enable2faAdmin, out var enable2faAdmin)) dto.Enable2faAdmin = bool.TryParse(enable2faAdmin, out var b2) ? b2 : false;

            if (map.TryGetValue(CommonConstants.SettingKeys.EnableEmailNotif, out var enableEmailNotif)) dto.EnableEmailNotif = bool.TryParse(enableEmailNotif, out var ben) ? ben : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifNewMember, out var notifNewMember)) dto.NotifNewMember = bool.TryParse(notifNewMember, out var bnm) ? bnm : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifPaymentConfirm, out var notifPaymentConfirm)) dto.NotifPaymentConfirm = bool.TryParse(notifPaymentConfirm, out var bpc) ? bpc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifEventReminder, out var notifEventReminder)) dto.NotifEventReminder = bool.TryParse(notifEventReminder, out var ber) ? ber : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.NotifSupportTicket, out var notifSupportTicket)) dto.NotifSupportTicket = bool.TryParse(notifSupportTicket, out var bst) ? bst : false;

            if (map.TryGetValue(CommonConstants.SettingKeys.QrReceiverName, out var qrReceiverName)) dto.QrReceiverName = qrReceiverName;
            if (map.TryGetValue(CommonConstants.SettingKeys.QrUpiId, out var qrUpiId)) dto.QrUpiId = qrUpiId;
            if (map.TryGetValue(CommonConstants.SettingKeys.QrImage, out var qrImage)) dto.QrImage = qrImage;

            if (map.TryGetValue(CommonConstants.SettingKeys.EnableAuditLogs, out var enableAuditLogs)) dto.EnableAuditLogs = bool.TryParse(enableAuditLogs, out var bal) ? bal : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogUserLogin, out var logUserLogin)) dto.LogUserLogin = bool.TryParse(logUserLogin, out var lul) ? lul : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogDataChanges, out var logDataChanges)) dto.LogDataChanges = bool.TryParse(logDataChanges, out var ldc) ? ldc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.LogConfigChanges, out var logConfigChanges)) dto.LogConfigChanges = bool.TryParse(logConfigChanges, out var lcc) ? lcc : true;
            if (map.TryGetValue(CommonConstants.SettingKeys.RetentionPeriod, out var retentionPeriod)) dto.RetentionPeriod = retentionPeriod;

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
            var dict = new Dictionary<string, (string Value, string Category)>
            {
                [CommonConstants.SettingKeys.OrgName] = (settings.OrgName, CommonConstants.SettingCategories.General),
                [CommonConstants.SettingKeys.BirthdayMembersExempt] = (settings.BirthdayMembersExempt.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.General),
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
                [CommonConstants.SettingKeys.QrReceiverName] = (settings.QrReceiverName, CommonConstants.SettingCategories.PaymentQr),
                [CommonConstants.SettingKeys.QrUpiId] = (settings.QrUpiId, CommonConstants.SettingCategories.PaymentQr),
                [CommonConstants.SettingKeys.QrImage] = (settings.QrImage, CommonConstants.SettingCategories.PaymentQr),
                [CommonConstants.SettingKeys.EnableAuditLogs] = (settings.EnableAuditLogs.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogUserLogin] = (settings.LogUserLogin.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogDataChanges] = (settings.LogDataChanges.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.LogConfigChanges] = (settings.LogConfigChanges.ToString().ToLowerInvariant(), CommonConstants.SettingCategories.Audit),
                [CommonConstants.SettingKeys.RetentionPeriod] = (settings.RetentionPeriod, CommonConstants.SettingCategories.Audit)
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
                    entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
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
                        CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                await _settingRepository.AddRangeAsync(toAdd, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            var defaults = new SystemSettingsDto();
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
}
