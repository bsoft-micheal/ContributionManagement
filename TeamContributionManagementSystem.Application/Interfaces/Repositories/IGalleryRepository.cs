using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Interfaces.Repositories;

public interface IGalleryRepository
{
    Task<List<GalleryPhoto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default);
    Task<GalleryPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default);
    Task AddAsync(GalleryPhoto photo, CancellationToken cancellationToken = default);
    void Delete(GalleryPhoto photo);

    // Standardized naming
    Task<List<GalleryPhoto>> GetAllGalleryAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, cancellationToken);
    Task<GalleryPhoto?> GetGalleryAsyncById(Guid photoId, CancellationToken cancellationToken = default) => GetByIdAsync(photoId, cancellationToken);
    Task SaveGalleryAsync(GalleryPhoto photo, CancellationToken cancellationToken = default) => AddAsync(photo, cancellationToken);
    void DeleteGalleryAsyncById(GalleryPhoto photo) => Delete(photo);
}
