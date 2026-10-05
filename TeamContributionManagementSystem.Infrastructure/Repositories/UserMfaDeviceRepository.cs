using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class UserMfaDeviceRepository : IUserMfaDeviceRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserMfaDeviceRepository> _logger;

    public UserMfaDeviceRepository(ApplicationDbContext context, ILogger<UserMfaDeviceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(UserMfaDevice device, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.UserMfaDevices.AddAsync(device, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public async Task<IEnumerable<UserMfaDevice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserMfaDevices
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.DateAdded)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByUserIdAsync));
            throw;
        }
    }

    public Task RemoveAsync(UserMfaDevice device, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.UserMfaDevices.Remove(device);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(RemoveAsync));
            throw;
        }
    }

    public async Task<UserMfaDevice?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserMfaDevices
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == deviceId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }
}
