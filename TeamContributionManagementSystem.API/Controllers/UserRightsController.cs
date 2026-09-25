using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Services;

namespace TeamContributionManagementSystem.API.Controllers;

/// <summary>
/// Manages granular access rights assigned to specific roles.
/// </summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route(CommonRoutes.UserRights.Base)]
public class UserRightsController : ControllerBase
{
    private readonly IRoleRightsService _roleRightsService;

    public UserRightsController(IRoleRightsService roleRightsService)
    {
        _roleRightsService = roleRightsService;
    }

    /// <summary>
    /// Retrieves a list of all rights mapped to roles.
    /// </summary>
    [HttpGet(CommonRoutes.UserRights.GetAll)]
    [ActionName(nameof(GetAllUserRightAsync))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleRightDto>>>> GetAllUserRightAsync(CancellationToken cancellationToken)
    {
        var result = await _roleRightsService.GetAllRoleRightAsync(cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<RoleRightDto>>.SuccessResult(result, CommonMessages.UserRights.GetAllSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Retrieves the access rights assigned to a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role (e.g., 'Admin').</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet(CommonRoutes.UserRights.GetByRole)]
    [ActionName(nameof(GetUserRightAsyncByRole))]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleRightDto>>>> GetUserRightAsyncByRole(string roleName, CancellationToken cancellationToken)
    {
        var result = await _roleRightsService.GetRoleRightAsyncByRole(roleName, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse<IReadOnlyCollection<RoleRightDto>>.SuccessResult(result, CommonMessages.UserRights.GetByRoleSuccess, CommonStatusCodes.Status200OK));
    }

    /// <summary>
    /// Saves or updates the granular access rights for a specific role.
    /// </summary>
    /// <param name="request">The role and its new rights configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost(CommonRoutes.UserRights.Save)]
    [ActionName(nameof(SaveUserRightAsync))]
    public async Task<ActionResult<ApiResponse>> SaveUserRightAsync([FromBody] UpdateRoleRightsRequestDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        await _roleRightsService.SaveRoleRightsAsync(request, currentUser, cancellationToken);
        return StatusCode(CommonStatusCodes.Status200OK, ApiResponse.SuccessResult(CommonMessages.UserRights.SaveSuccess, CommonStatusCodes.Status200OK));
    }
}
