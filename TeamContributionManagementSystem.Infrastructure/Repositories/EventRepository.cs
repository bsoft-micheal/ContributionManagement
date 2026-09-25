using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(ApplicationDbContext context, ILogger<EventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Event>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildEventQuery();

            if (month.HasValue)
            {
                query = query.Where(x => x.EventDate.Month == month.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(x => x.EventDate.Year == year.Value);
            }

            return await query
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.EventDate)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<Event>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            return await BuildEventQuery()
                .Where(x => !x.IsDeleted && x.EventDate >= DateTime.UtcNow.Date)
                .OrderBy(x => x.EventDate)
                .Take(count)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetUpcomingAsync));
            throw;
        }
    }

    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events.FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<Event?> GetByIdWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await BuildEventQuery()
                .FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdWithDetailsAsync));
            throw;
        }
    }

    public async Task<bool> BirthdayEventExistsAsync(Guid memberId, int month, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Events
                .Include(x => x.EventType)
                .AnyAsync(x =>
                    x.EventType != null &&
                    x.EventType.EventTypeName == CommonConstants.EventTypeNames.Birthday &&
                    x.EventDate.Month == month &&
                    x.EventDate.Year == year &&
                    x.Description.Contains(memberId.ToString()),
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(BirthdayEventExistsAsync));
            throw;
        }
    }

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Events.AddAsync(eventItem, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
            throw;
        }
    }

    public void Update(Event eventItem)
    {
        try
        {
            _context.Events.Update(eventItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }

    public void DeleteParticipants(IEnumerable<EventParticipant> participants)
    {
        try
        {
            _context.EventParticipants.RemoveRange(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteParticipants));
            throw;
        }
    }

    private IQueryable<Event> BuildEventQuery()
        => _context.Events
            .Include(x => x.EventType)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Participants)
                .ThenInclude(x => x.Member)
                    .ThenInclude(x => x!.Role)
            .Include(x => x.Contributions)
                .ThenInclude(x => x.Member);
}
