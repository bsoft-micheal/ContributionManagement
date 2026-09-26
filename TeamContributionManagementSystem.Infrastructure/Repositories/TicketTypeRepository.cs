using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.DTOs.TicketTypes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class TicketTypeRepository : ITicketTypeRepository
{
    private readonly ApplicationDbContext _context;

    public TicketTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<TicketTypeDto>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TicketTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.TypeName)
            .Select(x => new TicketTypeDto
            {
                TicketTypeId = x.TicketTypeId,
                TypeName = x.TypeName,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                CreatedOn = x.CreatedOn,
                ModifiedBy = x.ModifiedBy,
                ModifiedOn = x.ModifiedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TicketTypes
            .FirstOrDefaultAsync(x => x.TicketTypeId == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<TicketType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.TicketTypes
            .FirstOrDefaultAsync(x => x.TypeName.ToLower() == name.Trim().ToLower() && !x.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(TicketType ticketType, CancellationToken cancellationToken = default)
    {
        await _context.TicketTypes.AddAsync(ticketType, cancellationToken);
    }

    public void Update(TicketType ticketType)
    {
        _context.TicketTypes.Update(ticketType);
    }

    public void Delete(TicketType ticketType)
    {
        ticketType.IsDeleted = true;
        _context.TicketTypes.Update(ticketType);
    }
}
