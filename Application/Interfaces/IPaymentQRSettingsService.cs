using System.Collections.Generic;
using System.Threading.Tasks;
using TeamContributionManagementSystem.Application.DTOs;

namespace TeamContributionManagementSystem.Application.Interfaces
{
    public interface IPaymentQRSettingsService
    {
        Task<IEnumerable<PaymentQRSettingsDto>> GetAllPaymentQrSettingsAsync();
        Task<PaymentQRSettingsDto?> GetPaymentQrSettingByEventTypeAsync(string eventType);
        Task<PaymentQRSettingsDto?> GetPaymentQrSettingByEventTypeIdAsync(int eventTypeId);
        Task<PaymentQRSettingsDto> SavePaymentQrSettingAsync(SavePaymentQRSettingsRequest request);
    }
}
