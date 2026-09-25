using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class WorkTypeRepository : IWorkTypeRepository
{
    private readonly ApplicationDbContext _context;

    public WorkTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<WorkType>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.WorkTypeName).ToListAsync(cancellationToken);
    }

    public async Task<WorkType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkTypes
            .FirstOrDefaultAsync(x => x.WorkTypeId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<WorkType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.WorkTypes
            .FirstOrDefaultAsync(x => x.WorkTypeName.ToLower() == name.Trim().ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(WorkType workType, CancellationToken cancellationToken = default)
    {
        await _context.WorkTypes.AddAsync(workType, cancellationToken);
    }

    public void Update(WorkType workType)
    {
        _context.WorkTypes.Update(workType);
    }

    public void Delete(WorkType workType)
    {
        workType.IsDeleted = true;
        _context.WorkTypes.Update(workType);
    }
}
