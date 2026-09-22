using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class GalleryService : IGalleryService
{
    private readonly Microsoft.Extensions.Logging.ILogger<GalleryService> _logger;
    private readonly IGalleryRepository _galleryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GalleryService(Microsoft.Extensions.Logging.ILogger<GalleryService> logger, IGalleryRepository galleryRepository, IUnitOfWork unitOfWork, IMapper mapper)
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
            _logger.LogError(ex, "Error in GetAllAsync");
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
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Moments" : request.Category.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            TakenDate = request.TakenDate,
            Description = request.Description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? "System" : user,
            CreatedOn = DateTime.UtcNow
        };

        await _galleryRepository.AddAsync(photo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<GalleryPhotoDto>(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var photo = await _galleryRepository.GetByIdAsync(photoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Photo with ID {photoId} not found.");

        _galleryRepository.Delete(photo);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}
