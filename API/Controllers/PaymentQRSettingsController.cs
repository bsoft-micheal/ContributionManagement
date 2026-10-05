using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs;
using TeamContributionManagementSystem.Application.Interfaces;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class PaymentQRSettingsController : ControllerBase
    {
        private readonly IPaymentQRSettingsRepository _repository;

        public PaymentQRSettingsController(IPaymentQRSettingsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Retrieves all Payment QR settings across all Event Types (Birthday, Farewell, Team Dinner, Teamouting, etc.).
        /// Endpoint: GET /api/v1/settings/getAllPaymentQrSettingsAsync
        /// </summary>
        [HttpGet("settings/getAllPaymentQrSettingsAsync")]
        [HttpGet("payment-qr/all")]
        public async Task<ActionResult<IEnumerable<PaymentQRSettingsDto>>> GetAllPaymentQrSettings()
        {
            var entities = await _repository.GetAllAsync();
            var dtos = entities.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves the Payment QR configuration for a specific Event Type.
        /// Endpoint: GET /api/v1/settings/getPaymentQrSettingsAsync?eventType=Teamouting
        /// </summary>
        [HttpGet("settings/getPaymentQrSettingsAsync")]
        [HttpGet("payment-qr/{eventType}")]
        public async Task<ActionResult<PaymentQRSettingsDto>> GetPaymentQrSettingByEventType([FromQuery] string? eventType, [FromRoute] string? routeEventType)
        {
            var target = !string.IsNullOrWhiteSpace(eventType) ? eventType : routeEventType;
            if (string.IsNullOrWhiteSpace(target))
            {
                return BadRequest(new { message = "EventType parameter is required." });
            }

            var entity = await _repository.GetByEventTypeAsync(target);
            if (entity == null)
            {
                return NotFound(new { message = $"Payment QR is not configured for event type: {target}" });
            }

            return Ok(MapToDto(entity));
        }

        /// <summary>
        /// Saves or updates the Payment QR configuration for a specific Event Type using Repository logic.
        /// Endpoint: POST /api/v1/settings/savePaymentQrSettingAsync
        /// </summary>
        [HttpPost("settings/savePaymentQrSettingAsync")]
        [HttpPost("payment-qr/save")]
        public async Task<ActionResult<PaymentQRSettingsDto>> SavePaymentQrSetting([FromBody] SavePaymentQRSettingsRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Invalid request payload." });
            }

            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(new { message = "EventType is required." });
            }

            if (string.IsNullOrWhiteSpace(request.ReceiverName))
            {
                return BadRequest(new { message = "ReceiverName is required." });
            }

            if (string.IsNullOrWhiteSpace(request.UPIId))
            {
                return BadRequest(new { message = "UPIId is required." });
            }

            var entity = new PaymentQRSettings
            {
                EventType = request.EventType.Trim(),
                EventTypeId = request.EventTypeId,
                ReceiverName = request.ReceiverName.Trim(),
                UPIId = request.UPIId.Trim(),
                QRCodeMode = string.IsNullOrWhiteSpace(request.QRCodeMode) ? "generated" : request.QRCodeMode.Trim(),
                QRCodeImage = request.QRCodeImage,
                IsActive = request.IsActive
            };

            // Executes upsert logic strictly in repository
            var saved = await _repository.SaveOrUpdateAsync(entity);
            return Ok(MapToDto(saved));
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
