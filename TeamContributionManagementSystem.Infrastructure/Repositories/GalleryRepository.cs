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
                var cleanEvent = eventName.Trim().ToLower();
                query = query.Where(x => x.Event != null && x.Event.EventName.ToLower() == cleanEvent);
            }

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals(CommonConstants.PaymentStatuses.All, StringComparison.OrdinalIgnoreCase))
            {
                var cleanCat = category.Trim().ToLower();
                query = query.Where(x => x.Event != null && x.Event.EventType != null && x.Event.EventType.EventTypeName.ToLower() == cleanCat);
            }

            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await query
                .OrderByDescending(x => x.TakenDate)
                .Select(x => new GalleryPhotoDto
                {
                    PhotoId = x.PhotoId,
                    Title = x.Title,
                    EventName = x.Event != null ? x.Event.EventName : string.Empty,
                    Category = x.Event != null && x.Event.EventType != null ? x.Event.EventType.EventTypeName : string.Empty,
                    ImageUrl = x.ImageUrl,
                    TakenDate = x.TakenDate,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedAt,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && users.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && users.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
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
