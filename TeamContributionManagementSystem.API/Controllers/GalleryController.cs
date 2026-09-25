using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages photo gallery uploads and event album media.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Gallery.Base)]
public class GalleryController : ControllerBase
{
    private readonly IGalleryService _galleryService;

    public GalleryController(IGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    /// <summary>
    /// Retrieves a list of gallery photos.
    /// </summary>
    [HttpGet(CommonRoutes.Gallery.GetAll)]
    [ActionName(nameof(GetAllGalleryAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GalleryPhotoDto>>>> GetAllGalleryAsync(
        [FromQuery] string? eventName,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var result = await _galleryService.GetAllGalleryAsync(eventName, category, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<GalleryPhotoDto>>.SuccessResult(result, CommonMessages.Gallery.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Uploads a new photo to the gallery.
    /// </summary>
    [HttpPost(CommonRoutes.Gallery.Save)]
    [ActionName(nameof(SaveGalleryAsync))]
    public async Task<ActionResult<ApiResponse<GalleryPhotoDto>>> SaveGalleryAsync([FromBody] CreateGalleryPhotoRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _galleryService.SaveGalleryAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<GalleryPhotoDto>.SuccessResult(result, CommonMessages.Gallery.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Deletes a photo from the gallery (Admin or Manager).
    /// </summary>
    [Authorize(Roles = CommonRoles.AdminOrManager)]
    [HttpDelete(CommonRoutes.Gallery.Delete)]
    [ActionName(nameof(DeleteGalleryAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteGalleryAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _galleryService.DeleteGalleryAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Gallery.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
