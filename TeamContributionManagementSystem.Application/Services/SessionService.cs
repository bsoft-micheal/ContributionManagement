using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SessionService : ISessionService
{
    private readonly IDeviceSessionRepository _deviceSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(IDeviceSessionRepository deviceSessionRepository, IUnitOfWork unitOfWork)
    {
        _deviceSessionRepository = deviceSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DeviceDetail>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _deviceSessionRepository.GetActiveSessionsAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<DeviceDetail>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _deviceSessionRepository.GetSessionHistoryAsync(userId, cancellationToken);
    }

    public async Task LogoutSessionAsync(Guid historyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var history = await _deviceSessionRepository.GetLoginHistoryByIdAsync(historyId, cancellationToken);
        if (history == null || history.UserId != userId)
        {
            throw new KeyNotFoundException("Session not found or does not belong to the user.");
        }

        if (history.IsActive)
        {
            history.IsActive = false;
            history.LogoutTime = DateTime.UtcNow;

            if (history.DeviceDetail != null)
            {
                history.DeviceDetail.IsActive = false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
