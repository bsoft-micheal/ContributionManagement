using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamContributionManagementSystem.Application.DTOs;
using TeamContributionManagementSystem.Application.Interfaces;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services
{
    public class PaymentQRSettingsService : IPaymentQRSettingsService
    {
        private readonly IPaymentQRSettingsRepository _repository;

        public PaymentQRSettingsService(IPaymentQRSettingsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<PaymentQRSettingsDto>> GetAllPaymentQrSettingsAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToDto).ToList();
        }

        public async Task<PaymentQRSettingsDto?> GetPaymentQrSettingByEventTypeAsync(string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventType)) return null;

            var entity = await _repository.GetByEventTypeAsync(eventType);
            return entity != null ? MapToDto(entity) : null;
        }

        public async Task<PaymentQRSettingsDto?> GetPaymentQrSettingByEventTypeIdAsync(int eventTypeId)
        {
            var entity = await _repository.GetByEventTypeIdAsync(eventTypeId);
            return entity != null ? MapToDto(entity) : null;
        }

        public async Task<PaymentQRSettingsDto> SavePaymentQrSettingAsync(SavePaymentQRSettingsRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.EventType))
                throw new ArgumentException("EventType is required.", nameof(request.EventType));
            if (string.IsNullOrWhiteSpace(request.ReceiverName))
                throw new ArgumentException("ReceiverName is required.", nameof(request.ReceiverName));
            if (string.IsNullOrWhiteSpace(request.UPIId))
                throw new ArgumentException("UPIId is required.", nameof(request.UPIId));

            var entity = new PaymentQRSettings
            {
                EventType = request.EventType.Trim(),
                EventTypeId = request.EventTypeId,
                ReceiverName = request.ReceiverName.Trim(),
                UPIId = request.UPIId.Trim(),
                QRCodeMode = string.IsNullOrWhiteSpace(request.QRCodeMode) ? "generated" : request.QRCodeMode.Trim(),
                QRCodeImage = request.QRCodeImage,
                IsActive = request.IsActive,
            };

            // Repository handles all lookup, update/insert isolation logic
            var saved = await _repository.SaveOrUpdateAsync(entity);
            return MapToDto(saved);
        }

        private static PaymentQRSettingsDto MapToDto(PaymentQRSettings entity)
        {
            return new PaymentQRSettingsDto
            {
                Id = entity.Id,
                EventType = entity.EventType,
                EventTypeId = entity.EventTypeId,
                ReceiverName = entity.ReceiverName,
                UPIId = entity.UPIId,
                QRCodeMode = entity.QRCodeMode,
                QRCodeImage = entity.QRCodeImage,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate,
                IsActive = entity.IsActive,
            };
        }
    }
}
