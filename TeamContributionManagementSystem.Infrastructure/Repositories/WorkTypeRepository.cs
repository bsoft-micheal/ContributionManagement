using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.WorkTypes;
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

    public async Task<IReadOnlyCollection<WorkTypeDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var memberWorkTypes = await _context.Users
            .Where(m => !m.IsDeleted && m.WorkTypeId != null && m.WorkTypeNavigation != null)
            .Select(m => m.WorkTypeNavigation!.WorkTypeName.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _context.WorkTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var items = await query
            .OrderBy(x => x.WorkTypeName)
            .Select(x => new WorkTypeDto
            {
                WorkTypeId = x.WorkTypeId,
                WorkTypeName = x.WorkTypeName,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var cleanName = item.WorkTypeName.Trim().ToLower();
            item.IsReferred = memberWorkTypes.Contains(cleanName);
        }

        return items;
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

    public async Task<bool> HasMembersAsync(string workTypeName, CancellationToken cancellationToken = default)
    {
        var cleanName = workTypeName.Trim().ToLower();
        return await _context.Users.AnyAsync(m => !m.IsDeleted && m.WorkTypeNavigation != null && m.WorkTypeNavigation.WorkTypeName.ToLower() == cleanName, cancellationToken);
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
