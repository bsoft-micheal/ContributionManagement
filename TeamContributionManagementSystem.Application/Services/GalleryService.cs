using System.IO;
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
    private readonly IUserRepository? _userRepository;

    public GalleryService(
        ILogger<GalleryService> logger, 
        IGalleryRepository galleryRepository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IUserRepository? userRepository = null)
    {
        _logger = logger;
        _galleryRepository = galleryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<GalleryPhotoDto>> GetAllAsync(string? eventName = null, string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var dtos = (await _galleryRepository.GetAllAsync(eventName, category, cancellationToken)).ToList();
            if (_userRepository != null)
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    var userDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var u in users)
                    {
                        var displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
                        userDict[u.UserId.ToString()] = displayName;
                        userDict[u.Username] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.FullName))
                        {
                            userDict[u.FullName] = displayName;
                        }
                    }

                    foreach (var dto in dtos)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.CreatedBy))
                        {
                            if (userDict.TryGetValue(dto.CreatedBy, out var currentName))
                            {
                                dto.CreatedBy = currentName;
                            }
                            else
                            {
                                var matched = users.FirstOrDefault(u =>
                                    (!string.IsNullOrWhiteSpace(u.FullName) && (u.FullName.StartsWith(dto.CreatedBy, StringComparison.OrdinalIgnoreCase) || dto.CreatedBy.StartsWith(u.FullName, StringComparison.OrdinalIgnoreCase))) ||
                                    (!string.IsNullOrWhiteSpace(u.Username) && (u.Username.StartsWith(dto.CreatedBy, StringComparison.OrdinalIgnoreCase) || dto.CreatedBy.StartsWith(u.Username, StringComparison.OrdinalIgnoreCase))));

                                if (matched != null)
                                {
                                    var resolved = !string.IsNullOrWhiteSpace(matched.FullName) ? matched.FullName : matched.Username;
                                    var staleName = dto.CreatedBy;
                                    dto.CreatedBy = resolved;

                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await _userRepository.CascadeUpdateCreatorDisplayNameAsync(matched.UserId, staleName, resolved);
                                        }
                                        catch { }
                                    });
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(dto.ModifiedBy))
                        {
                            if (userDict.TryGetValue(dto.ModifiedBy, out var modName))
                            {
                                dto.ModifiedBy = modName;
                            }
                            else
                            {
                                var matched = users.FirstOrDefault(u =>
                                    (!string.IsNullOrWhiteSpace(u.FullName) && (u.FullName.StartsWith(dto.ModifiedBy, StringComparison.OrdinalIgnoreCase) || dto.ModifiedBy.StartsWith(u.FullName, StringComparison.OrdinalIgnoreCase))) ||
                                    (!string.IsNullOrWhiteSpace(u.Username) && (u.Username.StartsWith(dto.ModifiedBy, StringComparison.OrdinalIgnoreCase) || dto.ModifiedBy.StartsWith(u.Username, StringComparison.OrdinalIgnoreCase))));

                                if (matched != null)
                                {
                                    dto.ModifiedBy = !string.IsNullOrWhiteSpace(matched.FullName) ? matched.FullName : matched.Username;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve user names for gallery photos");
                }
            }

            return dtos;
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
            var creator = user?.Trim();
            if (_userRepository != null && !string.IsNullOrWhiteSpace(creator))
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    var matched = users.FirstOrDefault(u =>
                        string.Equals(u.UserId.ToString(), creator, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.Username, creator, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.FullName, creator, StringComparison.OrdinalIgnoreCase));
                    if (matched != null && !string.IsNullOrWhiteSpace(matched.FullName))
                    {
                        creator = matched.FullName;
                    }
                }
                catch { }
            }

            var photo = new GalleryPhoto
            {
                PhotoId = Guid.NewGuid(),
                Title = request.Title.Trim(),
                EventName = request.EventName.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? CommonConstants.Defaults.DefaultGalleryCategory : request.Category.Trim(),
                ImageUrl = await ProcessGalleryImageAsync(request.ImageUrl, cancellationToken),
                TakenDate = request.TakenDate,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(creator) ? null : creator,
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

    private async Task<string> ProcessGalleryImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;

        // Check if it's a JSON array of images
        if (imageUrl.TrimStart().StartsWith("["))
        {
            try
            {
                var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imageUrl);
                if (urls != null && urls.Count > 0)
                {
                    var savedList = new List<string>();
                    foreach (var u in urls)
                    {
                        savedList.Add(await SaveSingleGalleryImageAsync(u, cancellationToken));
                    }
                    return System.Text.Json.JsonSerializer.Serialize(savedList);
                }
            }
            catch { }
        }

        return await SaveSingleGalleryImageAsync(imageUrl, cancellationToken);
    }

    private async Task<string> SaveSingleGalleryImageAsync(string rawImage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawImage)) return string.Empty;
        var trimmed = rawImage.Trim();
        if (trimmed.StartsWith(CommonConstants.Defaults.DataImagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var commaIndex = trimmed.IndexOf(CommonConstants.Defaults.Comma);
                var base64Data = commaIndex >= 0 ? trimmed.Substring(commaIndex + 1) : trimmed;
                var imageBytes = Convert.FromBase64String(base64Data);

                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, "gallery_images");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var extension = CommonConstants.Defaults.ExtJpg;
                if (trimmed.Contains("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    extension = CommonConstants.Defaults.ExtPng;
                }

                var dateStr = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N")[..8];
                var fileName = $"{dateStr}_{uniqueId}.{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
                return $"/gallery_images/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save gallery image to disk");
            }
        }
        return trimmed;
    }
}
