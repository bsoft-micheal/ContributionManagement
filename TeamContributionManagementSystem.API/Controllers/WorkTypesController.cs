using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.WorkTypes;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages Support Data Work Types.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.WorkTypes.Base)]
public class WorkTypesController : ControllerBase
{
    private readonly IWorkTypeService _workTypeService;

    public WorkTypesController(IWorkTypeService workTypeService)
    {
        _workTypeService = workTypeService;
    }

    /// <summary>
    /// Retrieves all work types.
    /// </summary>
    [HttpGet(CommonRoutes.WorkTypes.GetAll)]
    [ActionName(nameof(GetAllWorkTypeAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<WorkTypeDto>>>> GetAllWorkTypeAsync([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        var result = await _workTypeService.GetAllWorkTypeAsync(activeOnly, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<WorkTypeDto>>.SuccessResult(result, CommonMessages.WorkTypes.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves a work type by ID.
    /// </summary>
    [HttpGet(CommonRoutes.WorkTypes.GetById)]
    [ActionName(nameof(GetWorkTypeAsyncById))]
    public async Task<ActionResult<ApiResponse<WorkTypeDto>>> GetWorkTypeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workTypeService.GetWorkTypeAsyncById(id, cancellationToken);
        if (result == null)
        {
            return StatusCode(CommonStatusCodes.Status404NotFound, ApiResponse<WorkTypeDto>.FailureResult(CommonMessages.General.NotFound, CommonStatusCodes.Status404NotFound));
        }
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<WorkTypeDto>.SuccessResult(result, CommonMessages.WorkTypes.GetByIdSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new work type (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPost(CommonRoutes.WorkTypes.Create)]
    [ActionName(nameof(SaveWorkTypeAsync))]
    public async Task<ActionResult<ApiResponse<WorkTypeDto>>> SaveWorkTypeAsync([FromBody] CreateWorkTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _workTypeService.SaveWorkTypeAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<WorkTypeDto>.SuccessResult(result, CommonMessages.WorkTypes.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates an existing work type (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpPut(CommonRoutes.WorkTypes.Update)]
    [ActionName(nameof(UpdateWorkTypeAsyncById))]
    public async Task<ActionResult<ApiResponse<WorkTypeDto>>> UpdateWorkTypeAsyncById(Guid id, [FromBody] UpdateWorkTypeRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _workTypeService.UpdateWorkTypeAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<WorkTypeDto>.SuccessResult(result, CommonMessages.WorkTypes.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a work type (Admin only).
    /// </summary>
    [Authorize(Roles = CommonRoles.Admin)]
    [HttpDelete(CommonRoutes.WorkTypes.Delete)]
    [ActionName(nameof(DeleteWorkTypeAsyncById))]
    public async Task<ActionResult<ApiResponse>> DeleteWorkTypeAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _workTypeService.DeleteWorkTypeAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.WorkTypes.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
