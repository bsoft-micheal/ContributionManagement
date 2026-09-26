using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class PriorityRepository : IPriorityRepository
{
    private readonly ApplicationDbContext _context;

    public PriorityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Priority>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Priorities
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.PriorityName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Priority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Priorities
            .FirstOrDefaultAsync(x => x.PriorityId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Priority?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Priorities
            .FirstOrDefaultAsync(x => x.PriorityName.ToLower() == name.ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Priority priority, CancellationToken cancellationToken = default)
    {
        await _context.Priorities.AddAsync(priority, cancellationToken);
    }

    public void Update(Priority priority)
    {
        _context.Priorities.Update(priority);
    }

    public void Delete(Priority priority)
    {
        priority.IsDeleted = true;
        _context.Priorities.Update(priority);
    }
}
