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
    private readonly IEventRepository? _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUserRepository? _userRepository;

    public GalleryService(
        ILogger<GalleryService> logger, 
        IGalleryRepository galleryRepository, 
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IUserRepository? userRepository = null,
        IEventRepository? eventRepository = null)
    {
        _logger = logger;
        _galleryRepository = galleryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
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
            Guid? creatorGuid = null;
            if (!string.IsNullOrWhiteSpace(user))
            {
                creatorGuid = CommonMethods.ParseNullableGuid(user);
                if (!creatorGuid.HasValue && _userRepository != null)
                {
                    try
                    {
                        var users = await _userRepository.GetAllAsync(cancellationToken);
                        var matched = users.FirstOrDefault(u =>
                            string.Equals(u.Username, user.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(u.FullName, user.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            creatorGuid = matched.UserId;
                        }
                    }
                    catch { }
                }
            }

            Guid? eventId = null;
            if (_eventRepository != null && !string.IsNullOrWhiteSpace(request.EventName))
            {
                try
                {
                    var matchedEvent = await _eventRepository.GetByNameAsync(request.EventName.Trim(), cancellationToken);
                    if (matchedEvent != null)
                    {
                        eventId = matchedEvent.EventId;
                    }
                }
                catch { }
            }

            var photo = new GalleryPhoto
            {
                PhotoId = Guid.NewGuid(),
                EventId = eventId,
                Title = request.Title.Trim(),
                EventName = request.EventName.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? CommonConstants.Defaults.DefaultGalleryCategory : request.Category.Trim(),
                ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
                TakenDate = request.TakenDate,
                Description = request.Description?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = creatorGuid,
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
