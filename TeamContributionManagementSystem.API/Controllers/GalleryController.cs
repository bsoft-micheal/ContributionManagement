using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gallery")]
public class GalleryController : ControllerBase
{
    private readonly IGalleryService _galleryService;

    public GalleryController(IGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GalleryPhotoDto>>> GetAll(
        [FromQuery] string? eventName,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var result = await _galleryService.GetAllAsync(eventName, category, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<GalleryPhotoDto>> Create([FromBody] CreateGalleryPhotoRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "User";
        var result = await _galleryService.CreateAsync(request, currentUser, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _galleryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
