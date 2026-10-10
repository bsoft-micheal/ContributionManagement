using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingRepository> _logger;

    public SystemSettingRepository(ApplicationDbContext context, ILogger<SystemSettingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SystemSettings.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(x => x.SettingKey.ToLower() == key.ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByKeyAsync));
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<SystemSetting> settings, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SystemSettings.AddRangeAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddRangeAsync));
            throw;
        }
    }

    public void Update(SystemSetting setting)
    {
        try
        {
            _context.SystemSettings.Update(setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public async Task<List<EventTypePaymentSetting>> GetAllEventTypePaymentSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureEventTypePaymentSettingsTableExistsAsync(cancellationToken);
            return await _context.EventTypePaymentSettings.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllEventTypePaymentSettingsAsync));
            return new List<EventTypePaymentSetting>();
        }
    }

    public async Task SyncEventTypePaymentSettingsAsync(string paymentQrConfigsJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentQrConfigsJson))
        {
            return;
        }

        try
        {
            await EnsureEventTypePaymentSettingsTableExistsAsync(cancellationToken);

            using var doc = System.Text.Json.JsonDocument.Parse(paymentQrConfigsJson);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return;
            }

            var eventTypes = await _context.EventTypes.ToListAsync(cancellationToken);
            var existingSettings = await _context.EventTypePaymentSettings.ToListAsync(cancellationToken);

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var eventTypeName = property.Name.Trim();
                if (string.IsNullOrWhiteSpace(eventTypeName)) continue;

                var item = property.Value;
                if (item.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                string rawQrMode = item.TryGetProperty("qrMode", out var qm) ? qm.GetString() ?? "generated" : "generated";
                bool isUploaded = rawQrMode.Equals("uploaded", StringComparison.OrdinalIgnoreCase);
                string qrMode = isUploaded ? "uploaded" : "generated";

                string receiverName = string.Empty;
                string upiId = string.Empty;
                string? qrImageUrl = null;

                if (isUploaded)
                {
                    if (item.TryGetProperty("qrImage", out var qi) && qi.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        qrImageUrl = qi.GetString();
                    }
                    else if (item.TryGetProperty("qrImageUrl", out var qiu) && qiu.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        qrImageUrl = qiu.GetString();
                    }
                }
                else
                {
                    if (item.TryGetProperty("receiverName", out var rn) && rn.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        receiverName = rn.GetString() ?? string.Empty;
                    }
                    if (string.IsNullOrWhiteSpace(receiverName) && item.TryGetProperty("qrReceiverName", out var qrn) && qrn.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        receiverName = qrn.GetString() ?? string.Empty;
                    }

                    if (item.TryGetProperty("upiId", out var ui) && ui.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        upiId = ui.GetString() ?? string.Empty;
                    }
                    if (string.IsNullOrWhiteSpace(upiId) && item.TryGetProperty("qrUpiId", out var qui) && qui.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        upiId = qui.GetString() ?? string.Empty;
                    }
                }

                bool isConfigured = isUploaded
                    ? !string.IsNullOrWhiteSpace(qrImageUrl)
                    : (!string.IsNullOrWhiteSpace(upiId) && !string.IsNullOrWhiteSpace(receiverName));

                if (item.TryGetProperty("isConfigured", out var ic) && ic.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False)
                {
                    isConfigured = ic.GetBoolean() && isConfigured;
                }

                var matchedEventType = eventTypes.FirstOrDefault(e => e.EventTypeName.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase));
                var matchedSetting = existingSettings.FirstOrDefault(s => s.EventTypeName.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase));

                if (matchedSetting != null)
                {
                    matchedSetting.EventTypeName = eventTypeName;
                    if (matchedEventType != null) matchedSetting.EventTypeId = matchedEventType.EventTypeId;
                    matchedSetting.ReceiverName = receiverName.Trim();
                    matchedSetting.UpiId = upiId.Trim();
                    matchedSetting.QrMode = qrMode;
                    matchedSetting.QrImageUrl = qrImageUrl;
                    matchedSetting.IsConfigured = isConfigured;
                    _context.EventTypePaymentSettings.Update(matchedSetting);
                }
                else
                {
                    var newSetting = new EventTypePaymentSetting
                    {
                        PaymentSettingId = Guid.NewGuid(),
                        EventTypeId = matchedEventType?.EventTypeId,
                        EventTypeName = eventTypeName,
                        ReceiverName = receiverName.Trim(),
                        UpiId = upiId.Trim(),
                        QrMode = qrMode,
                        QrImageUrl = qrImageUrl,
                        IsConfigured = isConfigured
                    };
                    await _context.EventTypePaymentSettings.AddAsync(newSetting, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync event_type_payment_settings: {Message}", ex.Message);
        }
    }

    private async Task EnsureEventTypePaymentSettingsTableExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.event_type_payment_settings (
                    payment_setting_id uuid PRIMARY KEY,
                    event_type_id uuid NULL,
                    event_type_name varchar(100) NOT NULL,
                    upi_id varchar(150) NOT NULL,
                    receiver_name varchar(150) NOT NULL,
                    qr_mode varchar(50) DEFAULT 'generated',
                    qr_image_url text NULL,
                    is_configured boolean DEFAULT true
                );";
            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EnsureEventTypePaymentSettingsTableExistsAsync warning: {Message}", ex.Message);
        }
    }
}
