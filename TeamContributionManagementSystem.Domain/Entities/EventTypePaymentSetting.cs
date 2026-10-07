using System;

namespace TeamContributionManagementSystem.Domain.Entities;

public class EventTypePaymentSetting
{
    public Guid PaymentSettingId { get; set; } = Guid.NewGuid();
    public Guid? EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string UpiId { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string QrMode { get; set; } = "generated";
    public string? QrImageUrl { get; set; }
    public bool IsConfigured { get; set; } = true;
}
