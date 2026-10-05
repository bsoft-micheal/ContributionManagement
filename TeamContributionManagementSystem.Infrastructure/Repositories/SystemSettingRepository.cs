using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class SystemSettingRepository : ISystemSettingRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingRepository> _logger;

    public SystemSettingRepository(ApplicationDbContext context, ILogger<SystemSettingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SystemSettings.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(x => x.SettingKey.ToLower() == key.ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByKeyAsync));
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<SystemSetting> settings, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SystemSettings.AddRangeAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddRangeAsync));
            throw;
        }
    }

    public void Update(SystemSetting setting)
    {
        try
        {
            _context.SystemSettings.Update(setting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }
}
