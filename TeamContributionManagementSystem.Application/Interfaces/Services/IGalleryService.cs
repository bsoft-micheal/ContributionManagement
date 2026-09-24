using TeamContributionManagementSystem.Application.DTOs.Gallery;

namespace TeamContributionManagementSystem.Application.Interfaces.Services;

public interface IGalleryService
{
    Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default);
    Task<GalleryPhotoDto> CreateAsync(CreateGalleryPhotoRequestDto request, string? user = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid photoId, CancellationToken cancellationToken = default);

    // Standardized terminology
    Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllGalleryAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default) => GetAllAsync(eventName, category, cancellationToken);
    Task<GalleryPhotoDto> SaveGalleryAsync(CreateGalleryPhotoRequestDto request, string? user = null, CancellationToken cancellationToken = default) => CreateAsync(request, user, cancellationToken);
    Task DeleteGalleryAsyncById(Guid photoId, CancellationToken cancellationToken = default) => DeleteAsync(photoId, cancellationToken);
}
