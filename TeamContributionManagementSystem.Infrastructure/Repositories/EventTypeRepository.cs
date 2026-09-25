using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class EventTypeRepository : IEventTypeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventTypeRepository> _logger;

    public EventTypeRepository(ApplicationDbContext context, ILogger<EventTypeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EventType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.EventTypes.OrderBy(x => x.EventTypeName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<EventType?> GetByIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.EventTypes.FirstOrDefaultAsync(x => x.EventTypeId == eventTypeId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<EventType?> GetByNameAsync(string eventTypeName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.EventTypes.FirstOrDefaultAsync(x => x.EventTypeName.ToLower() == eventTypeName.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByNameAsync));
            throw;
        }
    }

    public async Task<bool> HasEventsAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events.AnyAsync(x => x.EventTypeId == eventTypeId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(HasEventsAsync));
            throw;
        }
    }

    public async Task AddAsync(EventType eventType, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.EventTypes.AddAsync(eventType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(EventType eventType)
    {
        try
        {
            _context.EventTypes.Update(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void Delete(EventType eventType)
    {
        try
        {
            _context.EventTypes.Remove(eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }
}
