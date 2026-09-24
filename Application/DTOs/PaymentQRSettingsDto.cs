using System;

namespace TeamContributionManagementSystem.Application.DTOs
{
    public class PaymentQRSettingsDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int? EventTypeId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string UPIId { get; set; } = string.Empty;
        public string QRCodeMode { get; set; } = "generated";
        public string? QRCodeImage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(UPIId) && !string.IsNullOrWhiteSpace(ReceiverName);
    }

    public class SavePaymentQRSettingsRequest
    {
        public string EventType { get; set; } = string.Empty;
        public int? EventTypeId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string UPIId { get; set; } = string.Empty;
        public string QRCodeMode { get; set; } = "generated";
        public string? QRCodeImage { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
