using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly ApplicationDbContext _context;

    public MemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Member>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<List<Member>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<List<Member>> GetByIdsAsync(IEnumerable<Guid> memberIds, CancellationToken cancellationToken = default)
    {
        var ids = memberIds.ToList();
        return await _context.Members
            .Include(x => x.Role)
            .Where(x => ids.Contains(x.MemberId) && !x.IsDeleted && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Member?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default)
        => await _context.Members
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.MemberId == memberId && !x.IsDeleted, cancellationToken);

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Members
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() && !x.IsDeleted, cancellationToken);

    public async Task<List<Member>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default)
        => await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted && x.IsActive && x.DateOfBirth.Month == month)
            .OrderBy(x => x.DateOfBirth.Day)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
        => await _context.Members.AddAsync(member, cancellationToken);

    public void Update(Member member)
        => _context.Members.Update(member);
}

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Roles.OrderBy(x => x.RoleName).ToListAsync(cancellationToken);

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        => await _context.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
        => await _context.Roles.FirstOrDefaultAsync(x => x.RoleName.ToLower() == roleName.ToLower(), cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await _context.Roles.AddAsync(role, cancellationToken);

    public void Update(Role role)
        => _context.Roles.Update(role);
}

public class EventTypeRepository : IEventTypeRepository
{
    private readonly ApplicationDbContext _context;

    public EventTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventType>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.EventTypes.OrderBy(x => x.EventTypeName).ToListAsync(cancellationToken);

    public async Task<EventType?> GetByIdAsync(Guid eventTypeId, CancellationToken cancellationToken = default)
        => await _context.EventTypes.FirstOrDefaultAsync(x => x.EventTypeId == eventTypeId, cancellationToken);

    public async Task<EventType?> GetByNameAsync(string eventTypeName, CancellationToken cancellationToken = default)
        => await _context.EventTypes.FirstOrDefaultAsync(x => x.EventTypeName.ToLower() == eventTypeName.ToLower(), cancellationToken);

    public async Task AddAsync(EventType eventType, CancellationToken cancellationToken = default)
        => await _context.EventTypes.AddAsync(eventType, cancellationToken);

    public void Update(EventType eventType)
        => _context.EventTypes.Update(eventType);
}

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Event>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
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

    public async Task<List<Event>> GetUpcomingAsync(int count, CancellationToken cancellationToken = default)
        => await BuildEventQuery()
            .Where(x => !x.IsDeleted && x.EventDate >= DateTime.UtcNow.Date)
            .OrderBy(x => x.EventDate)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<Event?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _context.Events.FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);

    public async Task<Event?> GetByIdWithDetailsAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await BuildEventQuery()
            .FirstOrDefaultAsync(x => x.EventId == eventId && !x.IsDeleted, cancellationToken);

    public async Task<bool> BirthdayEventExistsAsync(Guid memberId, int month, int year, CancellationToken cancellationToken = default)
        => await _context.Events
            .Include(x => x.EventType)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.EventType != null &&
                x.EventType.EventTypeName == "Birthday" &&
                x.EventDate.Month == month &&
                x.EventDate.Year == year &&
                x.Description.Contains(memberId.ToString()),
                cancellationToken);

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
        => await _context.Events.AddAsync(eventItem, cancellationToken);

    public void Update(Event eventItem)
        => _context.Events.Update(eventItem);

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

public class ContributionRepository : IContributionRepository
{
    private readonly ApplicationDbContext _context;

    public ContributionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Contribution>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        => await _context.Contributions
            .Include(x => x.Event)
            .Include(x => x.Member)
            .Where(x => x.EventId == eventId && !x.IsDeleted)
            .OrderBy(x => x.Member!.Name)
            .ToListAsync(cancellationToken);

    public async Task<Contribution?> GetByEventAndMemberAsync(Guid eventId, Guid memberId, CancellationToken cancellationToken = default)
        => await _context.Contributions
            .Include(x => x.Event)
            .Include(x => x.Member)
            .FirstOrDefaultAsync(x => x.EventId == eventId && x.MemberId == memberId && !x.IsDeleted, cancellationToken);

    public async Task<List<Contribution>> GetPendingAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Contributions
            .Include(x => x.Event)
            .Include(x => x.Member)
            .Where(x => !x.IsDeleted && x.PaymentStatus != PaymentStatus.Paid);

        if (month.HasValue)
        {
            query = query.Where(x => x.Event != null && x.Event.EventDate.Month == month.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(x => x.Event != null && x.Event.EventDate.Year == year.Value);
        }

        return await query
            .OrderBy(x => x.Event!.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Contribution> contributions, CancellationToken cancellationToken = default)
        => await _context.Contributions.AddRangeAsync(contributions, cancellationToken);

    public void Update(Contribution contribution)
        => _context.Contributions.Update(contribution);
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<AppUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<AppUser?> GetFirstAdminAsync(CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(x => x.Role == UserRole.Admin && x.IsActive, cancellationToken);

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        => await _context.Users.AddAsync(user, cancellationToken);
}
