using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly ApplicationDbContext _context;

    public StatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<StatusDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Statuses
            .Where(x => !x.IsDeleted)
            .AsNoTracking();

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.StatusName)
            .Select(x => new StatusDto
            {
                StatusId = x.StatusId,
                StatusName = x.StatusName,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Status?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Status?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Statuses
            .FirstOrDefaultAsync(x => x.StatusName.ToLower() == name.ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Status status, CancellationToken cancellationToken = default)
    {
        await _context.Statuses.AddAsync(status, cancellationToken);
    }

    public void Update(Status status)
    {
        _context.Statuses.Update(status);
    }

    public void Delete(Status status)
    {
        status.IsDeleted = true;
        _context.Statuses.Update(status);
    }
}
