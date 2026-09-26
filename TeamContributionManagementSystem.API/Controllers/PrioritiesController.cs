using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Priorities;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Support Data Priorities.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Priorities.Base)]
public class PrioritiesController : ControllerBase
{
    private readonly IPriorityService _priorityService;

    public PrioritiesController(IPriorityService priorityService)
    {
        _priorityService = priorityService;
    }

    /// <summary>
    /// Retrieves all priorities.
    /// </summary>
    [HttpGet(CommonRoutes.Priorities.GetAll)]
    [ActionName(nameof(GetAllPriorityAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PriorityDto>>>> GetAllPriorityAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var result = await _priorityService.GetAllPriorityAsync(activeOnly, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<PriorityDto>>.SuccessResult(result, CommonMessages.Priorities.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a priority by ID.
    /// </summary>
    [HttpGet(CommonRoutes.Priorities.GetById)]
    [ActionName(nameof(GetPriorityAsyncById))]
    public async Task<ActionResult<ApiResponse<PriorityDto>>> GetPriorityAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _priorityService.GetPriorityAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<PriorityDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PriorityDto>.SuccessResult(result, CommonMessages.Priorities.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new priority (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Priorities.Create)]
    [ActionName(nameof(SavePriorityAsync))]
    public async Task<ActionResult<ApiResponse<PriorityDto>>> SavePriorityAsync([FromBody] CreatePriorityRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _priorityService.SavePriorityAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<PriorityDto>.SuccessResult(result, CommonMessages.Priorities.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing priority (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.Priorities.Update)]
    [ActionName(nameof(UpdatePriorityAsyncById))]
    public async Task<ActionResult<ApiResponse<PriorityDto>>> UpdatePriorityAsyncById(Guid id, [FromBody] UpdatePriorityRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _priorityService.UpdatePriorityAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<PriorityDto>.SuccessResult(result, CommonMessages.Priorities.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a priority (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.Priorities.Delete)]
    [ActionName(nameof(DeletePriorityAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeletePriorityAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _priorityService.DeletePriorityAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Priorities.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
