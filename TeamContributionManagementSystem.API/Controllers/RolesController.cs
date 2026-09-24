using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages system roles (e.g., Admin, User) that dictate user permissions.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// Retrieves a list of all defined roles in the system.
    /// </summary>
    [HttpGet("getAllRoleAsync")]
    [ActionName("GetAllRoleAsync")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> GetAllRoleAsync(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetAllRoleAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<RoleDto>>.SuccessResult(result, CommonMessages.Roles.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="request">The details of the role to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("saveRoleAsync")]
    [ActionName("SaveRoleAsync")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> SaveRoleAsync([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _roleService.SaveRoleAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status201Created, ApiResponse<RoleDto>.SuccessResult(result, CommonMessages.Roles.SaveSuccess, CommonStatusCodes.Status201Created));
    }

    /// <summary>
    /// Updates the name or description of an existing role.
    /// </summary>
    /// <param name="id">The unique identifier of the role.</param>
    /// <param name="request">The updated role details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("updateRoleAsyncById/{id:guid}")]
    [ActionName("UpdateRoleAsyncById")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRoleAsyncById(Guid id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await _roleService.UpdateRoleAsyncById(id, request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<RoleDto>.SuccessResult(result, CommonMessages.Roles.UpdateSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Deletes a role from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the role to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("deleteRoleAsyncById/{id:guid}")]
    [ActionName("DeleteRoleAsyncById")]
    public async Task<ActionResult<ApiResponse>> DeleteRoleAsyncById(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteRoleAsyncById(id, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.Roles.DeleteSuccess, CommonStatusCodes.Status200OK));
    }
}
