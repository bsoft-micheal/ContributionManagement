using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Infrastructure.Repositories
{
    /// <summary>
    /// Repository containing all database query, lookup, isolation, and upsert logic
    /// for per-event-type UPI Payment QR configurations.
    /// Nothing is hardcoded; all configs are dynamic and stored per EventType.
    /// </summary>
    public class PaymentQRSettingsRepository : IPaymentQRSettingsRepository
    {
        private readonly DbContext _context;
        private readonly DbSet<PaymentQRSettings> _dbSet;

        public PaymentQRSettingsRepository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<PaymentQRSettings>();
        }

        public async Task<IEnumerable<PaymentQRSettings>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PaymentQRSettings?> GetByEventTypeAsync(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType)) return null;

            var normalized = eventType.Trim();
            return await _dbSet
                .FirstOrDefaultAsync(x => x.EventType.ToLower() == normalized.ToLower());
        }

        public async Task<PaymentQRSettings?> GetByEventTypeIdAsync(int eventTypeId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(x => x.EventTypeId == eventTypeId);
        }

        /// <summary>
        /// Saves or updates the Payment QR configuration for a specific Event Type.
        /// Logic:
        /// 1. Searches for existing record by normalized EventType (or EventTypeId).
        /// 2. If existing found: updates only this event type's fields (ReceiverName, UPIId, QRCodeMode, QRCodeImage, IsActive, UpdatedDate).
        ///    Guarantees saving one event type (e.g. Birthday) NEVER overwrites another (e.g. Team Dinner).
        /// 3. If not found: inserts a new record with CreatedDate and the specified EventType.
        /// </summary>
        public async Task<PaymentQRSettings> SaveOrUpdateAsync(PaymentQRSettings setting)
        {
            if (setting == null) throw new ArgumentNullException(nameof(setting));
            if (string.IsNullOrWhiteSpace(setting.EventType))
                throw new ArgumentException("EventType is required.", nameof(setting.EventType));

            var normalizedEventType = setting.EventType.Trim();

            // Check if record exists for this event type
            var existing = await _dbSet
                .FirstOrDefaultAsync(x => x.EventType.ToLower() == normalizedEventType.ToLower() ||
                                         (setting.EventTypeId.HasValue && x.EventTypeId == setting.EventTypeId.Value));

            var processedQrImage = await ProcessQrImageAsync(setting.QRCodeImage);

            if (existing != null)
            {
                // Update only this specific event type record
                existing.EventType = normalizedEventType;
                if (setting.EventTypeId.HasValue) existing.EventTypeId = setting.EventTypeId;
                existing.ReceiverName = (setting.ReceiverName ?? "").Trim();
                existing.UPIId = (setting.UPIId ?? "").Trim();
                existing.QRCodeMode = string.IsNullOrWhiteSpace(setting.QRCodeMode) ? "generated" : setting.QRCodeMode.Trim();
                existing.QRCodeImage = processedQrImage;
                existing.IsActive = setting.IsActive;
                existing.UpdatedDate = DateTime.UtcNow;

                _dbSet.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                // Insert new record for this event type
                var newEntity = new PaymentQRSettings
                {
                    EventType = normalizedEventType,
                    EventTypeId = setting.EventTypeId,
                    ReceiverName = (setting.ReceiverName ?? "").Trim(),
                    UPIId = (setting.UPIId ?? "").Trim(),
                    QRCodeMode = string.IsNullOrWhiteSpace(setting.QRCodeMode) ? "generated" : setting.QRCodeMode.Trim(),
                    QRCodeImage = processedQrImage,
                    IsActive = setting.IsActive,
                    CreatedDate = DateTime.UtcNow,
                };

                await _dbSet.AddAsync(newEntity);
                await _context.SaveChangesAsync();
                return newEntity;
            }
        }

        private static async Task<string?> ProcessQrImageAsync(string? qrCodeImage)
        {
            if (string.IsNullOrWhiteSpace(qrCodeImage)) return qrCodeImage;
            var trimmed = qrCodeImage.Trim();
            if (trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var commaIndex = trimmed.IndexOf(",");
                    var base64Data = commaIndex >= 0 ? trimmed.Substring(commaIndex + 1) : trimmed;
                    var imageBytes = Convert.FromBase64String(base64Data);

                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qr_images");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    var extension = trimmed.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("image/jpg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                    var dateStr = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    var uniqueId = Guid.NewGuid().ToString("N")[..8];
                    var fileName = $"qr_{dateStr}_{uniqueId}.{extension}";
                    var filePath = Path.Combine(folderPath, fileName);

                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    return $"/qr_images/{fileName}";
                }
                catch { }
            }
            return trimmed;
        }

        public async Task<bool> DeleteAsync(string eventType)
        {
            var entity = await GetByEventTypeAsync(eventType);
            if (entity == null) return false;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
