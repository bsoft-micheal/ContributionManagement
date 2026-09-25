using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Support Data Statuses.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.Statuses.Base)]
public class StatusesController : ControllerBase
{
    private readonly IStatusService _statusService;

    public StatusesController(IStatusService statusService)
    {
        _statusService = statusService;
    }

    /// <summary>
    /// Retrieves all statuses.
    /// </summary>
    [HttpGet(CommonRoutes.Statuses.GetAll)]
    [ActionName(nameof(GetAllStatusAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<StatusDto>>>> GetAllStatusAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var result = await _statusService.GetAllStatusAsync(activeOnly, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<StatusDto>>.SuccessResult(result, CommonMessages.Statuses.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a status by ID.
    /// </summary>
    [HttpGet(CommonRoutes.Statuses.GetById)]
    [ActionName(nameof(GetStatusAsyncById))]
    public async Task<ActionResult<ApiResponse<StatusDto>>> GetStatusAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _statusService.GetStatusAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<StatusDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<StatusDto>.SuccessResult(result, CommonMessages.Statuses.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new status (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.Statuses.Create)]
    [ActionName(nameof(SaveStatusAsync))]
    public async Task<ActionResult<ApiResponse<StatusDto>>> SaveStatusAsync([FromBody] CreateStatusRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _statusService.SaveStatusAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<StatusDto>.SuccessResult(result, CommonMessages.Statuses.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing status (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.Statuses.Update)]
    [ActionName(nameof(UpdateStatusAsyncById))]
    public async Task<ActionResult<ApiResponse<StatusDto>>> UpdateStatusAsyncById(Guid id, [FromBody] UpdateStatusRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _statusService.UpdateStatusAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<StatusDto>.SuccessResult(result, CommonMessages.Statuses.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a status (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.Statuses.Delete)]
    [ActionName(nameof(DeleteStatusAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteStatusAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _statusService.DeleteStatusAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Statuses.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
