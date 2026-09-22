using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class GalleryRepository : IGalleryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<GalleryRepository> _logger;

    public GalleryRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<GalleryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GalleryPhoto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.GalleryPhotos.Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventName) && eventName != "ALL")
        {
            query = query.Where(x => x.EventName.ToLower() == eventName.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "ALL")
        {
            query = query.Where(x => x.Category.ToLower() == category.ToLower());
        }

        return await query.OrderByDescending(x => x.TakenDate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<GalleryPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.GalleryPhotos.FirstOrDefaultAsync(x => x.PhotoId == photoId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task AddAsync(GalleryPhoto photo, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.GalleryPhotos.AddAsync(photo, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddAsync");
            throw;
        }
    }

    public void Delete(GalleryPhoto photo)
    {
        try
        {
            photo.IsDeleted = true;
        photo.ModifiedOn = DateTime.UtcNow;
        _context.GalleryPhotos.Update(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            throw;
        }
    }
}
