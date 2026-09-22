using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class DeviceSessionRepository : IDeviceSessionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<DeviceSessionRepository> _logger;

    public DeviceSessionRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<DeviceSessionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void Add(DeviceDetail deviceDetail)
    {
        try
        {
            _context.DeviceDetails.Add(deviceDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Add");
            throw;
        }
    }

    public void Update(DeviceDetail deviceDetail)
    {
        try
        {
            _context.DeviceDetails.Update(deviceDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            throw;
        }
    }

    public async Task<DeviceDetail?> GetDeviceByDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeviceDetails
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetDeviceByDeviceIdAsync");
            throw;
        }
    }

    public async Task<IEnumerable<DeviceDetail>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeviceDetails
            .Include(d => d.LoginHistories.Where(h => h.IsActive))
            .Where(d => d.UserId == userId && d.IsActive)
            .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetActiveSessionsAsync");
            throw;
        }
    }

    public async Task<IEnumerable<DeviceDetail>> GetSessionHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeviceDetails
            .Include(d => d.LoginHistories.OrderByDescending(h => h.LoginTime))
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSessionHistoryAsync");
            throw;
        }
    }

    public async Task<DeviceLoginHistory?> GetLoginHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.DeviceLoginHistories
            .Include(h => h.DeviceDetail)
            .FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetLoginHistoryByIdAsync");
            throw;
        }
    }

    public void AddLoginHistory(DeviceLoginHistory loginHistory)
    {
        try
        {
            _context.DeviceLoginHistories.Add(loginHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddLoginHistory");
            throw;
        }
    }
}
