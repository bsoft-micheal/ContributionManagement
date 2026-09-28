using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.Application.Services;

public class SessionService : ISessionService
{
    private readonly ILogger<SessionService> _logger;
    private readonly IDeviceSessionRepository _deviceSessionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SessionService(ILogger<SessionService> logger, IDeviceSessionRepository deviceSessionRepository, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _deviceSessionRepository = deviceSessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DeviceSessionDto>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _deviceSessionRepository.GetActiveSessionsAsync(userId, cancellationToken);
            return sessions.Select(s => new DeviceSessionDto
            {
                Id = s.Id,
                DeviceId = s.DeviceId,
                DeviceName = s.DeviceName,
                Brand = s.Brand,
                Model = s.Model,
                Os = s.Os,
                OsVersion = s.OsVersion,
                SystemName = s.SystemName,
                SystemVersion = s.SystemVersion,
                DeviceType = s.DeviceType,
                AppVersion = s.AppVersion,
                Browser = s.Browser,
                BrowserVersion = s.BrowserVersion,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetActiveSessionsAsync));
            throw;
        }
    }

    public async Task<IEnumerable<DeviceSessionDto>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _deviceSessionRepository.GetSessionHistoryAsync(userId, cancellationToken);
            return history.Select(s => new DeviceSessionDto
            {
                Id = s.Id,
                DeviceId = s.DeviceId,
                DeviceName = s.DeviceName,
                Brand = s.Brand,
                Model = s.Model,
                Os = s.Os,
                OsVersion = s.OsVersion,
                SystemName = s.SystemName,
                SystemVersion = s.SystemVersion,
                DeviceType = s.DeviceType,
                AppVersion = s.AppVersion,
                Browser = s.Browser,
                BrowserVersion = s.BrowserVersion,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetSessionHistoryAsync));
            throw;
        }
    }

    public async Task LogoutSessionAsync(Guid historyId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _deviceSessionRepository.GetLoginHistoryByIdAsync(historyId, cancellationToken);
            if (history == null || history.UserId != userId)
            {
                throw new KeyNotFoundException(CommonMessages.DeviceInfo.SessionNotFound);
            }

            if (history.IsActive)
            {
                history.IsActive = false;
                history.LogoutTime = DateTime.UtcNow;

            if (history.DeviceDetail != null && history.DeviceDetail.DeviceType == 2)
            {
                history.DeviceDetail.IsActive = false;
            }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(LogoutSessionAsync));
            throw;
        }
    }
}
