using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class GalleryRepository : IGalleryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GalleryRepository> _logger;

    public GalleryRepository(ApplicationDbContext context, ILogger<GalleryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GalleryPhotoDto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.GalleryPhotos.Where(x => !x.IsDeleted).AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventName) && !eventName.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.EventName.ToLower() == eventName.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Category.ToLower() == category.ToLower());
            }

            return await query
                .OrderByDescending(x => x.TakenDate)
                .Select(x => new GalleryPhotoDto
                {
                    PhotoId = x.PhotoId,
                    Title = x.Title,
                    EventName = x.EventName,
                    Category = x.Category,
                    ImageUrl = x.ImageUrl,
                    TakenDate = x.TakenDate,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Delete));
            throw;
        }
    }
}
