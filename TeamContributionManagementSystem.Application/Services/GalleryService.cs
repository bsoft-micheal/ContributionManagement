using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class GalleryService : IGalleryService
{
    private readonly ILogger<GalleryService> _logger;
    private readonly IGalleryRepository _galleryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GalleryService(ILogger<GalleryService> logger, IGalleryRepository galleryRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _galleryRepository = galleryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var photos = await _galleryRepository.GetAllAsync(eventName, category, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<GalleryPhotoDto>>(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<GalleryPhotoDto> CreateAsync(CreateGalleryPhotoRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var photo = new GalleryPhoto
            {
                PhotoId = Guid.NewGuid(),
                Title = request.Title.Trim(),
                EventName = request.EventName.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? CommonConstants.Defaults.DefaultGalleryCategory : request.Category.Trim(),
                ImageUrl = request.ImageUrl.Trim(),
                TakenDate = request.TakenDate,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _galleryRepository.AddAsync(photo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Gallery.PhotoCreated, photo.Title, photo.PhotoId);
            return _mapper.Map<GalleryPhotoDto>(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var photo = await _galleryRepository.GetByIdAsync(photoId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Gallery.NotFound);

            _galleryRepository.Delete(photo);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Gallery.PhotoDeleted, photoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
