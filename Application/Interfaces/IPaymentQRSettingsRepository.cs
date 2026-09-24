using System.Collections.Generic;
using System.Threading.Tasks;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces
{
    public interface IPaymentQRSettingsRepository
    {
        Task<IEnumerable<PaymentQRSettings>> GetAllAsync();
        Task<PaymentQRSettings?> GetByEventTypeAsync(string eventType);
        Task<PaymentQRSettings?> GetByEventTypeIdAsync(int eventTypeId);
        Task<PaymentQRSettings> SaveOrUpdateAsync(PaymentQRSettings setting);
        Task<bool> DeleteAsync(string eventType);
    }
}
